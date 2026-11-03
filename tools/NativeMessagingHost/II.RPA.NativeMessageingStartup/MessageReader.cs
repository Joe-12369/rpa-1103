using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace II.RPA.NativeMessageingStartup
{
    internal class MessageReader : IDisposable
    {
        private readonly Stream input;
        private bool disposedValue;
        private readonly ILogger logger = Logger.OfContext();
        

        private const Int32 MAX_MESSAGE_SIZE = 30 * 1024 * 1024; // 1 GB

        public MessageReader(Stream stream)
        {
            input = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <summary>
        /// 从标准输入读取一条来自 Chrome 扩展的 Native Messaging 消息。
        /// Chrome 支持最大 4GB 消息体,我们限制为 1GB 以防滥用。
        /// </summary>
        /// <returns>
        /// - 成功：返回 UTF-8 JSON 字符串；
        /// - 浏览器关闭连接（EOF）：返回 null；
        /// - 其他错误：抛出 IOException。
        /// </returns>
        public string ReadMessage()
        {
            // === 1. 严格读满 4 字节长度头（小端序）===
            byte[] lenBytes = new byte[4];
            if (!TryReadExact(4, lenBytes))
            {
                logger.LogDebug("Browser closed the connection (EOF while reading header).");
                return null;
            }

            int messageLength = BitConverter.ToInt32(lenBytes, 0);

            // === 2. 极限值熔断，防止流失步解析出天文数字 ===
            if (messageLength <= 0 || messageLength > MAX_MESSAGE_SIZE)
            {
                // 抛出严重异常交由外层处理，防止恶意篡改或脏读击穿内存
                throw new IOException($"检测到异常的 Native Messaging 长度: {messageLength:N0} 字节。流已失步或超出 {MAX_MESSAGE_SIZE / (1024 * 1024)}MB 上限。");
            }

            // === 3. 分配缓冲区并严格读满完整内容 ===
            byte[] buffer;
            try
            {
                buffer = new byte[messageLength];
            }
            catch (OutOfMemoryException ex)
            {
                throw new IOException($"分配 {messageLength:N0} 字节失败。系统内存不足。", ex);
            }

            if (!TryReadExact(messageLength, buffer))
            {
                throw new IOException($"流被意外截断。预期需要 {messageLength} 字节，但流提前终止。");
            }

            // === 4. 解码为 UTF-8 字符串 ===
            try
            {
                return Encoding.UTF8.GetString(buffer);
            }
            catch (Exception ex)
            {
                throw new IOException("无法将消息数据解码为 UTF-8 字符串。", ex);
            }
        }

        /// <summary>
        /// 核心辅助方法：确保通过 while 循环严格读满请求的字节数，防御 I/O 截断
        /// </summary>
        private bool TryReadExact(int length, byte[] buffer)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int n = input.Read(buffer, totalRead, length - totalRead);
                if (n == 0)
                {
                    return false; // EOF
                }
                totalRead += n;
            }
            return true;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}