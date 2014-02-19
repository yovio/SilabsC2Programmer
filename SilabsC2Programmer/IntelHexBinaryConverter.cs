using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilabsC2Programmer
{
    public static class IntelHexBinaryConverter
    {
        public static void ConvertIntelHexToBinary(string pathToHexFile, string pathToBinFile)
        {
            var intelHexStructures = new List<IntelHexStructure>();
            using (var fs = File.OpenRead(pathToHexFile))
            using (StreamReader sr = new StreamReader(fs))
            {
                var firstchar = sr.Peek();

                if(firstchar != 58)
                    throw new ApplicationException(string.Format("{0} is not an IntelHexFile", pathToHexFile));

                var intelHex = new IntelHex();
                while (true)
                {
                    var oneLine = sr.ReadLine();
                    var intelHexStruct = intelHex.ConvertFromString(oneLine);
                    if (intelHexStruct.type == 0)
                        intelHexStructures.Add(intelHexStruct);
                    else
                        break;
                }
                sr.Close();
                fs.Close();
            }
            using (var fs = File.OpenWrite(@"C:\Users\yo02827\Downloads\bootloader.hm_trp.433.bin"))
            {
                for (int i = 0; i < intelHexStructures.Count; i++)
                {
                    var intelHexStruct = intelHexStructures[i];

                    fs.Write(intelHexStruct.data, 0, intelHexStruct.dataLen);

                    //Fill spaces with 0
                    if (i < intelHexStructures.Count - 1)
                    {
                        var nextIntelHextStruct = intelHexStructures[i + 1];
                        var fillLength = nextIntelHextStruct.address - (intelHexStruct.dataLen + intelHexStruct.address);
                        if (fillLength > 0)
                        {
                            fs.Write(new byte[fillLength], 0, fillLength);
                        }
                    }
                }
                fs.Flush();
                fs.Close();
            }
        }

        private static int s_MaxBytesToRead = 64;
        public static void ConvertBinaryToIntelHex(string pathToBinFile, string pathToHexFile, byte maxDataSize, byte numberOfZeroThreshold = 2)
        {
            int linesWritten = 0;
            try
            {
                
                ushort address = 0;
                IntelHex intelHex = new IntelHex();
                using (var fs = File.OpenRead(pathToBinFile))
                using (var fsHex = File.OpenWrite(pathToHexFile))
                {
                    int zeroCount = 0;
                    byte[] buffer = new byte[16];
                    byte bufferIndex = 0;
                    while (true)
                    {
                        var byteRead = fs.ReadByte();

                        if (byteRead == -1)
                            break;

                        buffer[bufferIndex] = (byte)byteRead;
                        bufferIndex++;
                        if (byteRead != 0)
                        {
                            zeroCount = 0;
                        }
                        else
                        {
                            zeroCount++;
                            if (zeroCount > numberOfZeroThreshold)
                            {
                                var intelHexStructure = intelHex.NewRecord(0, address, buffer, bufferIndex - zeroCount);
                                var intelHexString = intelHex.ConvertToString(intelHexStructure);
                                var bytesArray = ASCIIEncoding.ASCII.GetBytes(intelHexString);
                                fsHex.Write(bytesArray, 0, bytesArray.Length);
                                linesWritten++;
                                address += bufferIndex;

                                //loop until we found no zero
                                bool eof = false;
                                while (true)
                                {
                                    byteRead = fs.ReadByte();
                                    address++;
                                    if (byteRead == -1)
                                        eof = true;
                                    else if (byteRead > 0)
                                    {
                                        address--;
                                        fs.Position = address;
                                        buffer = new byte[16];
                                        bufferIndex = 0;
                                        break;
                                    }
                                    else
                                        zeroCount++;
                                }

                                if (eof)
                                    break;

                                zeroCount = 0;
                            }
                        }



                    }

                    var closingHexStructure = intelHex.NewRecord(1, 0, null, 0);
                    var closingBytesArray = ASCIIEncoding.ASCII.GetBytes(intelHex.ConvertToString(closingHexStructure));
                    fsHex.Write(closingBytesArray, 0, closingBytesArray.Length);

                    fs.Close();
                    fsHex.Flush();
                    fsHex.Close();
                }
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
