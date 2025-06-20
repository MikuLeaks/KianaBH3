﻿#if NEED_LINKEDLIST_SHIM
using LinkedListOfQueueItem = KcpSharp.NetstandardShim.LinkedList<(KcpSharp.KcpBuffer Data, byte Fragment)>;
using LinkedListNodeOfQueueItem = KcpSharp.NetstandardShim.LinkedListNode<(KcpSharp.KcpBuffer Data, byte Fragment)>;
#else
using LinkedListOfQueueItem =
    System.Collections.Generic.LinkedList<(KianaBH.KcpSharp.Base.KcpBuffer Data, byte Fragment)>;
#endif
using System.Diagnostics;
using System.Threading.Tasks.Sources;

namespace KianaBH.KcpSharp.Base;

internal sealed class KcpSendQueue : IValueTaskSource<bool>, IValueTaskSource, IDisposable
{
    private readonly IKcpBufferPool _bufferPool;
    private readonly KcpSendReceiveQueueItemCache _cache;
    private readonly int _capacity;
    private readonly int _mss;

    private readonly LinkedListOfQueueItem _queue;
    private readonly bool _stream;
    private readonly KcpConversationUpdateActivation _updateActivation;

    private bool _ackListNotEmpty;

    private bool _activeWait;
    private ReadOnlyMemory<byte> _buffer;
    private CancellationTokenRegistration _cancellationRegistration;
    private CancellationToken _cancellationToken;
    private bool _disposed;
    private bool _forStream;
    private ManualResetValueTaskSourceCore<bool> _mrvtsc;
    private byte _operationMode; // 0-send 1-flush 2-wait for space
    private bool _signled;

    private bool _transportClosed;
    private long _unflushedBytes;
    private int _waitForByteCount;
    private int _waitForSegmentCount;

    public KcpSendQueue(IKcpBufferPool bufferPool, KcpConversationUpdateActivation updateActivation, bool stream,
        int capacity, int mss, KcpSendReceiveQueueItemCache cache)
    {
        _bufferPool = bufferPool;
        _updateActivation = updateActivation;
        _stream = stream;
        _capacity = capacity;
        _mss = mss;
        _cache = cache;
        _mrvtsc = new ManualResetValueTaskSourceCore<bool>
        {
            RunContinuationsAsynchronously = true
        };

        _queue = new LinkedListOfQueueItem();
    }

    public void Dispose()
    {
        lock (_queue)
        {
            if (_disposed) return;
            if (_activeWait && !_signled)
            {
                if (_forStream)
                {
                    ClearPreviousOperation();
                    _mrvtsc.SetException(ThrowHelper.NewTransportClosedForStreamException());
                }
                else
                {
                    ClearPreviousOperation();
                    _mrvtsc.SetResult(false);
                }
            }

            var node = _queue.First;
            while (node is not null)
            {
                node.ValueRef.Data.Release();
                node = node.Next;
            }

            _queue.Clear();
            _disposed = true;
            _transportClosed = true;
        }
    }

    void IValueTaskSource.GetResult(short token)
    {
        try
        {
            _mrvtsc.GetResult(token);
        }
        finally
        {
            _mrvtsc.Reset();
            lock (_queue)
            {
                _activeWait = false;
                _signled = false;
                _cancellationRegistration = default;
            }
        }
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _mrvtsc.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags)
    {
        _mrvtsc.OnCompleted(continuation, state, token, flags);
    }

    bool IValueTaskSource<bool>.GetResult(short token)
    {
        _cancellationRegistration.Dispose();
        try
        {
            return _mrvtsc.GetResult(token);
        }
        finally
        {
            _mrvtsc.Reset();
            lock (_queue)
            {
                _activeWait = false;
                _signled = false;
                _cancellationRegistration = default;
            }
        }
    }

    public bool TryGetAvailableSpace(out int byteCount, out int segmentCount)
    {
        lock (_queue)
        {
            if (_transportClosed || _disposed)
            {
                byteCount = 0;
                segmentCount = 0;
                return false;
            }

            if (_activeWait && _operationMode == 0)
            {
                byteCount = 0;
                segmentCount = 0;
                return true;
            }

            GetAvailableSpaceCore(out byteCount, out segmentCount);
            return true;
        }
    }

    private void GetAvailableSpaceCore(out int byteCount, out int segmentCount)
    {
        var mss = _mss;
        var availableFragments = _capacity - _queue.Count;
        if (availableFragments < 0)
        {
            byteCount = 0;
            segmentCount = 0;
            return;
        }

        var availableBytes = availableFragments * mss;
        if (_stream)
        {
            var last = _queue.Last;
            if (last is not null) availableBytes += _mss - last.ValueRef.Data.Length;
        }

        byteCount = availableBytes;
        segmentCount = availableFragments;
    }

    public ValueTask<bool> WaitForAvailableSpaceAsync(int minimumBytes, int minimumSegments,
        CancellationToken cancellationToken)
    {
        short token;
        lock (_queue)
        {
            if (_transportClosed || _disposed)
            {
                minimumBytes = 0;
                minimumSegments = 0;
                return default;
            }

            if ((uint)minimumBytes > (uint)(_mss * _capacity))
                return new ValueTask<bool>(
                    Task.FromException<bool>(ThrowHelper.NewArgumentOutOfRangeException(nameof(minimumBytes))));
            if ((uint)minimumSegments > (uint)_capacity)
                return new ValueTask<bool>(
                    Task.FromException<bool>(ThrowHelper.NewArgumentOutOfRangeException(nameof(minimumSegments))));
            if (_activeWait)
                return new ValueTask<bool>(Task.FromException<bool>(ThrowHelper.NewConcurrentSendException()));
            if (cancellationToken.IsCancellationRequested)
                return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
            GetAvailableSpaceCore(out var currentByteCount, out var currentSegmentCount);
            if (currentByteCount >= minimumBytes && currentSegmentCount >= minimumSegments)
                return new ValueTask<bool>(true);

            _activeWait = true;
            Debug.Assert(!_signled);
            _forStream = false;
            _operationMode = 2;
            _waitForByteCount = minimumBytes;
            _waitForSegmentCount = minimumSegments;
            _cancellationToken = cancellationToken;
            token = _mrvtsc.Version;
        }

        _cancellationRegistration =
            cancellationToken.UnsafeRegister(state => ((KcpSendQueue?)state)!.SetCanceled(), this);

        return new ValueTask<bool>(this, token);
    }

    public bool TrySend(ReadOnlySpan<byte> buffer, bool allowPartialSend, out int bytesWritten)
    {
        lock (_queue)
        {
            if (allowPartialSend && !_stream) ThrowHelper.ThrowAllowPartialSendArgumentException();
            if (_transportClosed || _disposed)
            {
                bytesWritten = 0;
                return false;
            }

            var mss = _mss;
            // Make sure there is enough space.
            if (!allowPartialSend)
            {
                var spaceAvailable = mss * (_capacity - _queue.Count);
                if (spaceAvailable < 0)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (_stream)
                {
                    var last = _queue.Last;
                    if (last is not null) spaceAvailable += mss - last.ValueRef.Data.Length;
                }

                if (buffer.Length > spaceAvailable)
                {
                    bytesWritten = 0;
                    return false;
                }
            }

            // Copy buffer content.
            bytesWritten = 0;
            if (_stream)
            {
                var node = _queue.Last;
                if (node is not null)
                {
                    ref var data = ref node.ValueRef.Data;
                    var expand = mss - data.Length;
                    expand = Math.Min(expand, buffer.Length);
                    if (expand > 0)
                    {
                        data = data.AppendData(buffer.Slice(0, expand));
                        buffer = buffer.Slice(expand);
                        Interlocked.Add(ref _unflushedBytes, expand);
                        bytesWritten = expand;
                    }
                }

                if (buffer.IsEmpty) return true;
            }

            var anySegmentAdded = false;
            var count = buffer.Length <= mss ? 1 : (buffer.Length + mss - 1) / mss;
            Debug.Assert(count >= 1);
            while (count > 0 && _queue.Count < _capacity)
            {
                var fragment = --count;

                var size = buffer.Length > mss ? mss : buffer.Length;

                var owner = _bufferPool.Rent(new KcpBufferPoolRentOptions(mss, false));
                var kcpBuffer = KcpBuffer.CreateFromSpan(owner, buffer.Slice(0, size));
                buffer = buffer.Slice(size);

                _queue.AddLast(_cache.Rent(kcpBuffer, _stream ? (byte)0 : (byte)fragment));
                Interlocked.Add(ref _unflushedBytes, size);
                bytesWritten += size;
                anySegmentAdded = true;
            }

            if (anySegmentAdded) _updateActivation.Notify();
            return anySegmentAdded;
        }
    }

    public ValueTask<bool> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        short token;
        lock (_queue)
        {
            if (_transportClosed || _disposed) return new ValueTask<bool>(false);
            if (_activeWait)
                return new ValueTask<bool>(Task.FromException<bool>(ThrowHelper.NewConcurrentSendException()));
            if (cancellationToken.IsCancellationRequested)
                return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));

            var mss = _mss;
            if (_stream)
            {
                var node = _queue.Last;
                if (node is not null)
                {
                    ref var data = ref node.ValueRef.Data;
                    var expand = mss - data.Length;
                    expand = Math.Min(expand, buffer.Length);
                    if (expand > 0)
                    {
                        data = data.AppendData(buffer.Span.Slice(0, expand));
                        buffer = buffer.Slice(expand);
                        Interlocked.Add(ref _unflushedBytes, expand);
                    }
                }

                if (buffer.IsEmpty) return new ValueTask<bool>(true);
            }

            var count = buffer.Length <= mss ? 1 : (buffer.Length + mss - 1) / mss;
            Debug.Assert(count >= 1);

            if (!_stream && count > 256)
                return new ValueTask<bool>(Task.FromException<bool>(ThrowHelper.NewMessageTooLargeForBufferArgument()));

            // synchronously put fragments into queue.
            while (count > 0 && _queue.Count < _capacity)
            {
                var fragment = --count;

                var size = buffer.Length > mss ? mss : buffer.Length;
                var owner = _bufferPool.Rent(new KcpBufferPoolRentOptions(mss, false));
                var kcpBuffer = KcpBuffer.CreateFromSpan(owner, buffer.Span.Slice(0, size));
                buffer = buffer.Slice(size);

                _queue.AddLast(_cache.Rent(kcpBuffer, _stream ? (byte)0 : (byte)fragment));
                Interlocked.Add(ref _unflushedBytes, size);
            }

            _updateActivation.Notify();

            if (count == 0) return new ValueTask<bool>(true);

            _activeWait = true;
            Debug.Assert(!_signled);
            _forStream = false;
            _operationMode = 0;
            _buffer = buffer;
            _cancellationToken = cancellationToken;
            token = _mrvtsc.Version;
        }

        _cancellationRegistration =
            cancellationToken.UnsafeRegister(state => ((KcpSendQueue?)state)!.SetCanceled(), this);

        return new ValueTask<bool>(this, token);
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        short token;
        lock (_queue)
        {
            if (_transportClosed || _disposed)
                return new ValueTask(Task.FromException(ThrowHelper.NewTransportClosedForStreamException()));
            if (_activeWait) return new ValueTask(Task.FromException(ThrowHelper.NewConcurrentSendException()));
            if (cancellationToken.IsCancellationRequested) return new ValueTask(Task.FromCanceled(cancellationToken));

            var mss = _mss;
            if (_stream)
            {
                var node = _queue.Last;
                if (node is not null)
                {
                    ref var data = ref node.ValueRef.Data;
                    var expand = mss - data.Length;
                    expand = Math.Min(expand, buffer.Length);
                    if (expand > 0)
                    {
                        data = data.AppendData(buffer.Span.Slice(0, expand));
                        buffer = buffer.Slice(expand);
                        Interlocked.Add(ref _unflushedBytes, expand);
                    }
                }

                if (buffer.IsEmpty) return default;
            }

            var count = buffer.Length <= mss ? 1 : (buffer.Length + mss - 1) / mss;
            Debug.Assert(count >= 1);

            Debug.Assert(_stream);
            // synchronously put fragments into queue.
            while (count > 0 && _queue.Count < _capacity)
            {
                var size = buffer.Length > mss ? mss : buffer.Length;
                var owner = _bufferPool.Rent(new KcpBufferPoolRentOptions(mss, false));
                var kcpBuffer = KcpBuffer.CreateFromSpan(owner, buffer.Span.Slice(0, size));
                buffer = buffer.Slice(size);

                _queue.AddLast(_cache.Rent(kcpBuffer, 0));
                Interlocked.Add(ref _unflushedBytes, size);
            }

            _updateActivation.Notify();

            if (count == 0) return default;

            _activeWait = true;
            Debug.Assert(!_signled);
            _forStream = true;
            _operationMode = 0;
            _buffer = buffer;
            _cancellationToken = cancellationToken;
            token = _mrvtsc.Version;
        }

        _cancellationRegistration =
            cancellationToken.UnsafeRegister(state => ((KcpSendQueue?)state)!.SetCanceled(), this);

        return new ValueTask(this, token);
    }

    public ValueTask<bool> FlushAsync(CancellationToken cancellationToken)
    {
        short token;
        lock (_queue)
        {
            if (_transportClosed || _disposed) return new ValueTask<bool>(false);
            if (_activeWait)
                return new ValueTask<bool>(Task.FromException<bool>(ThrowHelper.NewConcurrentSendException()));
            if (cancellationToken.IsCancellationRequested)
                return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));

            _activeWait = true;
            Debug.Assert(!_signled);
            _forStream = false;
            _operationMode = 1;
            _buffer = default;
            _cancellationToken = cancellationToken;
            token = _mrvtsc.Version;
        }

        _cancellationRegistration =
            cancellationToken.UnsafeRegister(state => ((KcpSendQueue?)state)!.SetCanceled(), this);

        return new ValueTask<bool>(this, token);
    }

    public ValueTask FlushForStreamAsync(CancellationToken cancellationToken)
    {
        short token;
        lock (_queue)
        {
            if (_transportClosed || _disposed)
                return new ValueTask(Task.FromException(ThrowHelper.NewTransportClosedForStreamException()));
            if (_activeWait) return new ValueTask(Task.FromException(ThrowHelper.NewConcurrentSendException()));
            if (cancellationToken.IsCancellationRequested) return new ValueTask(Task.FromCanceled(cancellationToken));

            _activeWait = true;
            Debug.Assert(!_signled);
            _forStream = true;
            _operationMode = 1;
            _buffer = default;
            _cancellationToken = cancellationToken;
            token = _mrvtsc.Version;
        }

        _cancellationRegistration =
            cancellationToken.UnsafeRegister(state => ((KcpSendQueue?)state)!.SetCanceled(), this);

        return new ValueTask(this, token);
    }

    public bool CancelPendingOperation(Exception? innerException, CancellationToken cancellationToken)
    {
        lock (_queue)
        {
            if (_activeWait && !_signled)
            {
                ClearPreviousOperation();
                _mrvtsc.SetException(
                    ThrowHelper.NewOperationCanceledExceptionForCancelPendingSend(innerException, cancellationToken));
                return true;
            }
        }

        return false;
    }

    private void SetCanceled()
    {
        lock (_queue)
        {
            if (_activeWait && !_signled)
            {
                var cancellationToken = _cancellationToken;
                ClearPreviousOperation();
                _mrvtsc.SetException(new OperationCanceledException(cancellationToken));
            }
        }
    }

    private void ClearPreviousOperation()
    {
        _signled = true;
        _forStream = false;
        _operationMode = 0;
        _buffer = default;
        _waitForByteCount = default;
        _waitForSegmentCount = default;
        _cancellationToken = default;
    }

    public bool TryDequeue(out KcpBuffer data, out byte fragment)
    {
        lock (_queue)
        {
            var node = _queue.First;
            if (node is null)
            {
                data = default;
                fragment = default;
                return false;
            }

            (data, fragment) = node.ValueRef;
            _queue.RemoveFirst();
            node.ValueRef = default;
            _cache.Return(node);

            MoveOneSegmentIn();
            CheckForAvailableSpace();
            return true;
        }
    }

    public void NotifyAckListChanged(bool itemsListNotEmpty)
    {
        lock (_queue)
        {
            if (_transportClosed || _disposed) return;

            _ackListNotEmpty = itemsListNotEmpty;
            TryCompleteFlush(Interlocked.Read(ref _unflushedBytes));
        }
    }

    private void MoveOneSegmentIn()
    {
        if (_activeWait && !_signled && _operationMode == 0)
        {
            var buffer = _buffer;
            var mss = _mss;
            var count = buffer.Length <= mss ? 1 : (buffer.Length + mss - 1) / mss;

            var size = buffer.Length > mss ? mss : buffer.Length;
            var owner = _bufferPool.Rent(new KcpBufferPoolRentOptions(mss, false));
            var kcpBuffer = KcpBuffer.CreateFromSpan(owner, buffer.Span.Slice(0, size));
            _buffer = buffer.Slice(size);

            _queue.AddLast(_cache.Rent(kcpBuffer, _stream ? (byte)0 : (byte)(count - 1)));
            Interlocked.Add(ref _unflushedBytes, size);

            if (count == 1)
            {
                ClearPreviousOperation();
                _mrvtsc.SetResult(true);
            }
        }
    }

    private void CheckForAvailableSpace()
    {
        if (_activeWait && !_signled && _operationMode == 2)
        {
            GetAvailableSpaceCore(out var byteCount, out var segmentCount);
            if (byteCount >= _waitForByteCount && segmentCount >= _waitForSegmentCount)
            {
                ClearPreviousOperation();
                _mrvtsc.SetResult(true);
            }
        }
    }

    private void TryCompleteFlush(long unflushedBytes)
    {
        if (_activeWait && !_signled && _operationMode == 1)
            if (_queue.Last is null && unflushedBytes == 0 && !_ackListNotEmpty)
            {
                ClearPreviousOperation();
                _mrvtsc.SetResult(true);
            }
    }

    public void SubtractUnflushedBytes(int size)
    {
        var unflushedBytes = Interlocked.Add(ref _unflushedBytes, -size);
        if (unflushedBytes == 0)
            lock (_queue)
            {
                TryCompleteFlush(0);
            }
    }

    public long GetUnflushedBytes()
    {
        if (_transportClosed || _disposed) return 0;
        return Interlocked.Read(ref _unflushedBytes);
    }

    public void SetTransportClosed()
    {
        lock (_queue)
        {
            if (_transportClosed || _disposed) return;
            if (_activeWait && !_signled)
            {
                if (_forStream)
                {
                    ClearPreviousOperation();
                    _mrvtsc.SetException(ThrowHelper.NewTransportClosedForStreamException());
                }
                else
                {
                    ClearPreviousOperation();
                    _mrvtsc.SetResult(false);
                }
            }

            _transportClosed = true;
            Interlocked.Exchange(ref _unflushedBytes, 0);
        }
    }
}