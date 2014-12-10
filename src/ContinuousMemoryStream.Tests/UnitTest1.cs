using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace JBe.IO.Tests
{
    public class ContinuousMemoryStreamTests : IDisposable
    {
        private Thread writeThread;
        private Thread readThread;

        private const string data =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla eu lobortis justo, ut tempor sapien. Donec quam neque, tincidunt a gravida at, tempus sed dui. Etiam nec vehicula erat. Nullam semper semper pulvinar. Integer eleifend neque elit, vitae ultrices eros accumsan in. Fusce pharetra euismod neque. Suspendisse accumsan augue justo, at dictum lorem dignissim consequat. Quisque rutrum urna ante, id commodo ipsum accumsan non. Aliquam efficitur ante volutpat molestie pulvinar. In risus enim, euismod malesuada efficitur at, accumsan id ipsum. Suspendisse volutpat, nunc ut lacinia varius, risus sem semper purus, id scelerisque diam nisl congue augue. Maecenas interdum nibh sit.";

        [Fact]
        public void OneTreadWritesOneTreadReadsAllWritenData()
        {

            string receivedData = "";

            ContinuousMemoryStream stream = new ContinuousMemoryStream(2, 2);

            int writtentotal = 0;
            int readtotal = 0;
            writeThread = new Thread(() =>
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                stream.Write(bytes, 0, bytes.Length);
                writtentotal = bytes.Length;
                stream.SetEndOfStream();
            });
            writeThread.Start();

            readThread = new Thread(() =>
            {
                int read;
                byte[] buffer = new byte[64];
                List<byte> bytesList = new List<byte>();

                do
                {
                    readtotal += read = stream.Read(buffer, 0, buffer.Length);
                    for (int i = 0; i < read; i++)
                    {
                        bytesList.Add(buffer[i]);
                    }
                } while (read > 0);

                receivedData = Encoding.UTF8.GetString(bytesList.ToArray());

            });
            readThread.Start();


            writeThread.Join();
            readThread.Join();

            Assert.Equal(writtentotal, readtotal);
            Assert.Equal(data, receivedData);
        }

        public void Dispose()
        {
            writeThread.Join();
            readThread.Join();
        }
    }
}
