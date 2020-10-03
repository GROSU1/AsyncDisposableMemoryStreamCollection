using System;
using System.IO;
using System.Threading.Tasks;

namespace AsyncDisposableMemoryStreamCollection
{
    public sealed class DisposableMemoryStreamsCollection : System.IAsyncDisposable
    {
        private readonly Microsoft.IO.RecyclableMemoryStreamManager _streamManager;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<System.Guid, MemoryStream> _streams;
        private bool _disposed;

        /// <summary>
        /// Creates new instance of Disposable Streams Collection
        /// </summary>
        public DisposableMemoryStreamsCollection()
        {
            _streamManager = new Microsoft.IO.RecyclableMemoryStreamManager();
            _streams = new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, MemoryStream>();
        }

        /// <summary>
        /// Creates new instance of Disposable Streams Collection <br/>
        /// Refer to <seealso cref="RecyclableMemoryStreamManager"/> and docs <a href="https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream">here</a> for more info on Recycable Memory Streams
        /// </summary>
        /// <param name="blockSize">Size of each block of memory to be pooled by recycling streams manager (must be > 0)</param>
        /// <param name="largeBlockSizeMultiple">Large buffers in recycling streams manager will be multiple of the provided value</param>
        /// <param name="maximumBufferSize">Maximum size of pooled buffers that will be pooled by recycling streams manager (anything above are not pooled)</param>
        /// <param name="useExponentialLargeBuffer">Whether to use exponential large buffer in recycling streams manager</param>        
        public DisposableMemoryStreamsCollection(int blockSize, int largeBlockSizeMultiple, int maximumBufferSize, bool useExponentialLargeBuffer)
        {
            _streamManager = new Microsoft.IO.RecyclableMemoryStreamManager(blockSize, largeBlockSizeMultiple, maximumBufferSize, useExponentialLargeBuffer);
            _streams = new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, MemoryStream>();
        }

        /// <summary>
        /// Returns the number of Streams in the object
        /// </summary>
        public int Count => _streams.Count;

        /// <summary>
        /// Attempts to add the specified data to the collection using provided unique identifier
        /// </summary>
        /// <param name="identifier">Unique Guid used for identifying the data</param>
        /// <param name="data">The data to be added to the collection</param>
        /// <returns>Provided Guid that references the data entered or Empty Guid if data already exists in collection</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.OverflowException"></exception>
        public System.Guid TryAddData(System.Guid identifier, byte[] data)
        {
            var hasBeenAdded = _streams.TryAdd(identifier, _streamManager.GetStream(identifier, null, data));
            return hasBeenAdded ? identifier : System.Guid.Empty;
        }

        /// <summary>
        /// Attempts to add the specified data to the collection
        /// </summary>
        /// <param name="data">The data to be added to the collection</param>
        /// <returns>Unique Guid that references the data entered or Empty Guid if data already exists in collection</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.OverflowException"></exception>
        public System.Guid TryAddData(byte[] data) => TryAddData(System.Guid.NewGuid(), data);

        /// <summary>
        /// Attempts to retrieves the stream data from the collection
        /// </summary>
        /// <param name="identifier">Unique Guid used for identifying the data</param>
        /// <returns>Unmanaged byte array data requested or NULL if not found</returns>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.NotSupportedException"/>
        /// <exception cref="System.ObjectDisposedException"/>
        /// <exception cref="System.InvalidOperationException"/>
        public async Task<byte[]> TryGetDataAsync(System.Guid identifier)
        {
            var found = _streams.TryGetValue(identifier, out var stream);
            byte[] buffer = null;

            if (found)
            {
                buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
                stream.Position = 0;
            }

            return buffer;
        }

        /// <summary>
        /// Attempts to remove data associated with a unique identifier in the collection
        /// </summary>
        /// <param name="identifier">Unique Guid used for identifying the data</param>
        /// <returns>True if stream has been removed, otherwise False</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ObjectDisposedException"/>
        public async Task<bool> TryRemoveDataAsync(System.Guid identifier)
        {
            MemoryStream stream = null;
            bool hasBeenRemoved = false;

            try
            {
                hasBeenRemoved = _streams.TryRemove(identifier, out stream);
            }
            finally
            {
                if (stream != null) await stream.DisposeAsync().ConfigureAwait(false);
            }

            return hasBeenRemoved;
        }

        /// <summary>
        /// Update existing data in the collection with new data
        /// </summary>
        /// <param name="identifier">Unique Guid used for identifying the data</param>
        /// <param name="data">The data to be updated in the collection</param>
        /// <returns>True if data update was successful, otherwise False</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ObjectDisposedException"/>
        public async Task<bool> TryUpdateDataAsync(System.Guid identifier, byte[] data)
        {
            MemoryStream oldStream = null;
            bool hasBeenUpdate = false;

            try
            {
                var gotOldStream = _streams.TryGetValue(identifier, out oldStream);
                if (!gotOldStream) return false;

                hasBeenUpdate = _streams.TryUpdate(identifier, _streamManager.GetStream(identifier, null, data), oldStream);
            }
            finally
            {
                if (oldStream != null) await oldStream.DisposeAsync().ConfigureAwait(false);
            }

            return hasBeenUpdate;            
        }

        public async Task ClearAsync()
        {
            await DisposeStreamsAsync().ConfigureAwait(false);
            _streams.Clear();
            _disposed = false;
        }

        public ValueTask DisposeAsync() => new ValueTask(DisposeStreamsAsync());

        private Task DisposeStreamsAsync()
        {
            if (_disposed) return Task.CompletedTask;
            _disposed = true;

            var tasks = new System.Collections.Generic.List<Task>();
            foreach (var stream in _streams.Values) tasks.Add(stream.DisposeAsync().AsTask());
            return Task.WhenAll(tasks);
        }
    }
}