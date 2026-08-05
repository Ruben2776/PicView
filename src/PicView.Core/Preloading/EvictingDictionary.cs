using System.Collections;
using System.Diagnostics.CodeAnalysis;

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
    /// <param name="totalCount">The total count of the file list being iterated.</param>
    /// <param name="isReverse">
    /// Indicates navigation direction. If <see langword="true"/>, the item farthest ahead is evicted;
    /// if <see langword="false"/>, the item farthest behind is evicted.
    /// </param>
    /// <param name="evictedValue">
    /// When this method returns, contains the value evicted due to capacity limits or index replacement, 
    /// or <see langword="null"/> if no eviction occurred.
    /// </param>
    /// <param name="isNewReference">
    /// When this method returns, contains <see langword="true"/> if the value was newly inserted 
    /// or replaced a different object reference; otherwise, <see langword="false"/> if the key already 
    /// contained the exact same object reference.
    /// </param>
    /// <remarks>
    /// If <paramref name="key"/> already exists and holds the exact same reference, no modification 
    /// or eviction occurs and <paramref name="isNewReference"/> returns <see langword="false"/>.
    /// </remarks>
    public void TryAdd(int key, TValue value, int totalCount, bool isReverse, out TValue? evictedValue, out bool isNewReference)
    {
        _lock.Enter(); 
        try
        {
            isNewReference = true; // Assume new unless proven otherwise
            
            // Case 1: Key exists
            if (_dictionary.TryGetValue(key, out var existingValue))
            {
                if (ReferenceEquals(existingValue, value))
                {
                    isNewReference = false; // It's the same image, no new reference needed!
                    evictedValue = default;
                    return; 
                }
            
                // Replacing a DIFFERENT image. The old one is evicted!
                _dictionary[key] = value;
                evictedValue = existingValue;
                return;
            }

            if (_dictionary.Count >= _maxSize)
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
                    TValue removedValue;
                    if (isReverse)
                    {
                        var max = _dictionary.Keys.Max();
                        removedValue = _dictionary[max];
                        _dictionary.Remove(max);
                    }
                    else
                    {
                        var min = _dictionary.Keys.Min();
                        removedValue = _dictionary[min];
                        _dictionary.Remove(min);
                    }
                    evictedValue = removedValue;
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

            // Add the new item
            _dictionary.Add(key, value);
        }
        finally
        {
            _lock.Exit(); 
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
            snapshot = [.. _dictionary];
        }
        finally
        {
            _lock.Exit();
        }

        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}