using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
    class BufferGenerator
    {
        private readonly int bufferSize;

        public BufferGenerator(int bufferSize)
        {
            this.bufferSize = bufferSize;
        }

        public Task<ItemBuffer> GenerateBuffer()
        {
            return Task.Factory.StartNew(() =>
            {
                var itemBuffer = new ItemBuffer();
                var itemProvider = new ItemProvider();
                while (itemBuffer.AddNext(itemProvider.CreateNew()) < bufferSize)
                {
                }

                return itemBuffer;
            });

        }
    }
}
