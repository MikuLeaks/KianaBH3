﻿using System.Runtime.CompilerServices;

namespace KianaBH.KcpSharp.Base;

internal sealed class KcpAcknowledgeList
{
    private readonly KcpSendQueue _sendQueue;
    private (uint SerialNumber, uint Timestamp)[] _array;
    private int _count;
    private SpinLock _lock;

    public KcpAcknowledgeList(KcpSendQueue sendQueue, int windowSize)
    {
        _array = new (uint SerialNumber, uint Timestamp)[windowSize];
        _count = 0;
        _lock = new SpinLock();
        _sendQueue = sendQueue;
    }

    public bool TryGetAt(int index, out uint serialNumber, out uint timestamp)
    {
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            if ((uint)index >= (uint)_count)
            {
                serialNumber = default;
                timestamp = default;
                return false;
            }

            (serialNumber, timestamp) = _array[index];
            return true;
        }
        finally
        {
            if (lockTaken) _lock.Exit();
        }
    }

    public void Clear()
    {
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            _count = 0;
        }
        finally
        {
            if (lockTaken) _lock.Exit();
        }

        _sendQueue.NotifyAckListChanged(false);
    }

    public void Add(uint serialNumber, uint timestamp)
    {
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            EnsureCapacity();
            _array[_count++] = (serialNumber, timestamp);
        }
        finally
        {
            if (lockTaken) _lock.Exit();
        }

        _sendQueue.NotifyAckListChanged(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity()
    {
        if (_count == _array.Length) Expand();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Expand()
    {
        var capacity = _count + 1;
        capacity = Math.Max(capacity + capacity / 2, 16);
        var newArray = new (uint SerialNumber, uint Timestamp)[capacity];
        _array.AsSpan(0, _count).CopyTo(newArray);
        _array = newArray;
    }
}