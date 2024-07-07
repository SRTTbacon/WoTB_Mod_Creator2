using System.Text;
using K4os.Compression.LZ4;
using Force.Crc32;

namespace WoTB_Mod_Creator2.Class
{
    public class DVPL
    {
        /// <summary>
        /// Class for data from DVPL footer
        /// </summary>

        public class DVPLFooterData
        {
            /// <summary>
            /// The original (de- or not yet compressed) size of the file.
            /// </summary>
            public uint OSize { get; set; }
            /// <summary>
            /// The compressed size of the file. Footer data is not included.
            /// </summary>
            public uint CSize { get; set; }
            /// <summary>
            /// The CRC32 summ of compressed file. Footer data is not included.
            /// </summary>
            public uint Crc32 { get; set; }
            /// <summary>
            /// The type of compression. Usually equals 2, but for .tex files it is 0.
            /// </summary>
            public uint Type { get; set; }
        }

        /// <summary>
        /// Reads DVPL footer and returns its data.
        /// </summary>
        /// <param name="buffer">Array of bytes (usually from file), where program will search data.</param>
        /// <returns>DVPL footer data, using its own class.</returns>
        public static DVPLFooterData ReadDVPLFooter(byte[] buffer)
        {
            //easy guide to edit arrays in csharp lol
            byte[] footerBuffer = buffer.Reverse().Take(20).Reverse().ToArray();

            byte[] DVPLTypeBytes = footerBuffer.Reverse().Take(4).Reverse().ToArray();
            string DVPLTypeCheck = Encoding.UTF8.GetString(DVPLTypeBytes);
            if (DVPLTypeCheck != "DVPL") throw new Exception("Invalid DVPL Footer");

            DVPLFooterData dataThatWereRead = new()
            {
                OSize = BitConverter.ToUInt32(footerBuffer, 0),
                CSize = BitConverter.ToUInt32(footerBuffer, 4),
                Crc32 = BitConverter.ToUInt32(footerBuffer, 8),
                Type = BitConverter.ToUInt32(footerBuffer, 12)
            };

            return dataThatWereRead;
        }

        /// <summary>
        /// Decompresses given array of bytes. It's better to use it with try...catch, because there are a lot of things that can cause exceptions.
        /// </summary>
        /// <param name="buffer">Array of bytes to decompress (usually from file)</param>
        /// <returns>Decompressed array of bytes</returns>
        public static byte[] DecompressDVPL(byte[] buffer)
        {
            DVPLFooterData footerData = ReadDVPLFooter(buffer);
            byte[] targetBlock = buffer.Reverse().Skip(20).Reverse().ToArray();

            if (targetBlock.Length != footerData.CSize) throw new Exception("DVPL Size Mismatch");
            if (Crc32Algorithm.Compute(targetBlock) != footerData.Crc32) throw new Exception("DVPL CRC32 Mismatch");

            if (footerData.Type == 0)
            {
                if (!(footerData.OSize == footerData.CSize && footerData.Type == 0))
                {
                    throw new Exception("DVPL Compression Type 0 Size Mismatch");
                }
                else
                {
                    return targetBlock;
                }
            }
            else if (footerData.Type == 1 || footerData.Type == 2)
            {
                byte[] deDVPLBlock = new byte[footerData.OSize];
                int i = LZ4Codec.Decode(targetBlock, deDVPLBlock);

                if (i == -1) throw new Exception("DVPL Decoded Size Mismatch");

                return deDVPLBlock;
            }
            else throw new Exception("Unknown Format");
        }
    }
}
