using System.Collections;
using System.Diagnostics.CodeAnalysis;
using PicView.Core.DebugTools;

namespace PicView.Core.Preloading;

/// <summary>
/// A thread-safe, fixed-size dictionary keyed by <see cref="int"/> that evicts items
/// according to a directional policy intended for image iteration.
/// </summary>
public class EvictingDictionary<TValue> : IEnumerable<KeyValuePair<int, TValue>>
{
    private readonly Dictionary<int, TValue> _dictionary;
    private readonly Lock _lock = new(); // The lock object for thread safety
    private readonly int _maxSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TValue}"/> class.
    /// </summary>
    /// <param name="maxSize">The maximum number of items the dictionary can contain.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxSize"/> is less than or equal to zero.
    /// </exception>
    public EvictingDictionary(int maxSize)
    {
        if (maxSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Size must be positive.");
        }

        _maxSize = maxSize;
        _dictionary = new Dictionary<int, TValue>(maxSize);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <returns>The value associated with the specified <paramref name="key"/>.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the specified <paramref name="key"/> does not exist in the dictionary.
    /// </exception>
    /// <remarks>
    /// The indexer has no setter to ensure additions go through the directional eviction API.
    /// </remarks>
    public TValue this[int key]
    {
        get
        {
            _lock.Enter();
            try
            {
                return _dictionary[key];
            }
            finally
            {
                _lock.Exit();
            }
        }
    }

    /// <summary>
    /// Attempts to add a key/value pair, evicting an item if capacity is exceeded according to
    /// the supplied navigation direction.
    /// </summary>
    /// <param name="key">The unique integer key to add.</param>
    /// <param name="value">The value to associate with <paramref name="key"/>.</param>
    /// <param name="totalCount">The total count of the file list</param>
    /// <param name="isReverse">
    /// Indicates navigation direction. If <see langword="true"/>, the highest key is evicted;
    /// if <see langword="false"/>, the lowest key is evicted.
    /// </param>
    /// <param name="evictedValue">
    /// When this method returns, contains the value of the item that was evicted if an eviction occurred;
    /// otherwise, the default value for <typeparamref name="TValue"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an item was evicted due to capacity; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If <paramref name="key"/> already exists, its value is updated and no eviction occurs
    /// (this method returns <see langword="false"/> and <paramref name="evictedValue"/> is set to default).
    /// </remarks>
    public bool TryAdd(int key, TValue value, int totalCount, bool isReverse, [MaybeNullWhen(false)] out TValue evictedValue)
    {
        _lock.Enter(); // Lock acquired
        try
        {
            // If the key already exists, just update its value. No eviction.
            if (_dictionary.ContainsKey(key))
            {
                _dictionary[key] = value;
                evictedValue = default;
                return false;
            }

            if (_dictionary.Count > _maxSize)
            {
                // Looping Eviction Logic: Find the key farthest away from the current index.
                var keyToEvict = -1;
                var maxDistance = -1;

                if (isReverse)
                {
                    // Moving backward: Evict the key that is "farthest ahead".
                    // This is the key with the largest forward distance from the current index.
                    foreach (var dictionaryKey in _dictionary.Keys)
                    {
                        var distance = (dictionaryKey - key + totalCount) % totalCount;
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            keyToEvict = dictionaryKey;
                        }
                    }
                }
                else
                {
                    // Moving forward: Evict the key that is "farthest behind".
                    // This is the key with the largest backward distance from the current index.
                    foreach (var dictionaryKey in _dictionary.Keys)
                    {
                        var distance = (key - dictionaryKey + totalCount) % totalCount;
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            keyToEvict = dictionaryKey;
                        }
                    }
                }

                if (keyToEvict < 0 || !_dictionary.TryGetValue(keyToEvict, out var value1))
                {
                    if (isReverse)
                    {
                        _dictionary.Remove(_dictionary.Keys.Max());
                    }
                    else
                    {
                        _dictionary.Remove(_dictionary.Keys.Min());
                    }
                    evictedValue = default;
#if DEBUG
                    DebugHelper.LogDebug(nameof(EvictingDictionary<>), nameof(TryAdd), $"Invalid parameter for key {keyToEvict}");
#endif
                    
                }
                else
                {
                    evictedValue = value1;
                    _dictionary.Remove(keyToEvict);
                }
            }
            else
            {
                evictedValue = default;
            }

            _dictionary.Add(key, value);

            return !EqualityComparer<TValue>.Default.Equals(evictedValue, default);
        }
        finally
        {
            _lock.Exit(); // Lock released
        }
    }


    /// <summary>
    /// Removes the element with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Remove(int key)
    {
        _lock.Enter();
        try
        {
            return _dictionary.Remove(key);
        }
        finally
        {
            _lock.Exit();
        }
    }

    /// <summary>
    /// Removes the element with the specified key and returns the associated value.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the removed key,
    /// if the key is found; otherwise, the default value for <typeparamref name="TValue"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the element is found and removed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Remove(int key, [MaybeNullWhen(false)] out TValue value)
    {
        _lock.Enter();
        try
        {
            return _dictionary.Remove(key, out value);
        }
        finally
        {
            _lock.Exit();
        }
    }

    #region Unchanged Thread-Safe Methods

    /// <summary>
    /// Gets a snapshot of the keys contained in the dictionary.
    /// </summary>
    public ICollection<int> Keys
    {
        get
        {
            _lock.Enter();
            try
            {
                return _dictionary.Keys.ToArray();
            }
            finally
            {
                _lock.Exit();
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of the values contained in the dictionary.
    /// </summary>
    public ICollection<TValue> Values
    {
        get
        {
            _lock.Enter();
            try
            {
                return _dictionary.Values.ToArray();
            }
            finally
            {
                _lock.Exit();
            }
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the dictionary.
    /// </summary>
    public int Count
    {
        get
        {
            _lock.Enter();
            try
            {
                return _dictionary.Count;
            }
            finally
            {
                _lock.Exit();
            }
        }
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>
    /// <see langword="true"/> if the dictionary contains an element with the specified key;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsKey(int key)
    {
        _lock.Enter();
        try
        {
            return _dictionary.ContainsKey(key);
        }
        finally
        {
            _lock.Exit();
        }
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, the default value for <typeparamref name="TValue"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the dictionary contains an element with the specified key;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetValue(int key, [MaybeNullWhen(false)] out TValue value)
    {
        _lock.Enter();
        try
        {
            return _dictionary.TryGetValue(key, out value);
        }
        finally
        {
            _lock.Exit();
        }
    }

    /// <summary>
    /// Removes all elements from the dictionary.
    /// </summary>
    public void Clear()
    {
        _lock.Enter();
        try
        {
            _dictionary.Clear();
        }
        finally
        {
            _lock.Exit();
        }
    }


    /// <summary>
    /// Returns an enumerator that iterates through a snapshot of the dictionary.
    /// </summary>
    /// <returns>
    /// An enumerator for a point-in-time snapshot of the dictionary’s contents.
    /// </returns>
    /// <remarks>
    /// Enumeration does not hold the internal lock. The snapshot may not reflect subsequent changes.
    /// </remarks>
    public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator()
    {
        List<KeyValuePair<int, TValue>> snapshot;
        _lock.Enter();
        try
        {
            snapshot = _dictionary.ToList();
        }
        finally
        {
            _lock.Exit();
        }

        return snapshot.GetEnumerator();
    }


    /// <summary>
    /// Returns an enumerator that iterates through a snapshot of the dictionary.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}