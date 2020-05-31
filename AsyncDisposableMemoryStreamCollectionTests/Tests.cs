using System.Threading.Tasks;
using AsyncDisposableMemoryStreamCollection;
using Xunit;

namespace AsyncDisposableMemoryStreamTests
{
    public class Tests
    {
        [Fact]
        public async Task AddData()
        {
            await using var collection = new DisposableMemoryStreamsCollection();
            var identifier = collection.TryAddData(RandomBytes());
            Assert.NotEqual(System.Guid.Empty, identifier);
        }

        [Fact]
        public async Task GetData()
        {
            await using var collection = new DisposableMemoryStreamsCollection();
            var expectedData = RandomBytes();
            var identifier = collection.TryAddData(expectedData);            
            
            var retrievedData = await collection.TryGetDataAsync(identifier).ConfigureAwait(false);

            Assert.Equal(expectedData, retrievedData);
        }

        [Fact]
        public async Task GetDataMultipleTimes()
        {
            await using var collection = new DisposableMemoryStreamsCollection();
            var expectedData = RandomBytes();
            var identifier = collection.TryAddData(expectedData);

            var randomTimes = new System.Random().Next(2, 9);
            byte[] retrievedData = null;
            for (int i = 0; i < randomTimes; i++) retrievedData = await collection.TryGetDataAsync(identifier).ConfigureAwait(false);

            Assert.Equal(expectedData, retrievedData);
        }

        [Fact]
        public async Task RemoveData()
        {
            await using var collection = new DisposableMemoryStreamsCollection();
            var expectedData = RandomBytes();
            var identifier = collection.TryAddData(expectedData);                    

            var hasDataBeenRemoved = await collection.TryRemoveDataAsync(identifier).ConfigureAwait(false);

            Assert.True(hasDataBeenRemoved);
        }

        [Fact]
        public async Task UpdateData()
        {
            await using var collection = new DisposableMemoryStreamsCollection();
            var randomData = RandomBytes();
            var updateData = RandomBytes();
            var identifier = collection.TryAddData(randomData);

            var hasDataBeenUpdated = await collection.TryUpdateDataAsync(identifier,updateData).ConfigureAwait(false);
            var returnedUpdatedData = await collection.TryGetDataAsync(identifier).ConfigureAwait(false);

            Assert.Equal(updateData, returnedUpdatedData);
        }

        [Fact]
        public async Task ClearData()
        {
            await using var collection = new DisposableMemoryStreamsCollection();
            var randomData = RandomBytes();

            for (int i = 0; i < 3; i++) collection.TryAddData(randomData);

            await collection.ClearAsync().ConfigureAwait(false);
            Assert.Equal(0, collection.Count);
        }

        private static byte[] RandomBytes(int length = 1000000)
        {
            var str = new System.Text.StringBuilder();

            do
            {
                str.Append(System.Guid.NewGuid().ToString().Replace("-", "", System.StringComparison.CurrentCulture));
            }
            while (length > str.Length);

            return System.Text.Encoding.UTF8.GetBytes(str.ToString(0, length));
        }
    }
}
