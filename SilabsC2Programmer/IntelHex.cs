using System;
using System.CodeDom.Compiler;
using System.Text;
using System.IO;

namespace SilabsC2Programmer
{
    /// <summary>
    /// IntelHexStructure provides the internal data structure which will be used by the IntelHex class.
    /// This class is used for internal processing and is declared public to allow the application that instantiates
    /// the IntelHex class access to the internal storage.
    /// </summary>
    public class IntelHexStructure
    {
        public UInt16 address;  //< The 16-bit address field.
        //< The 8-bit array data field, which has a maximum size of 256 bytes.
        public byte[] data = new byte[IntelHex.IHEX_MAX_DATA_LEN / 2];
        public int dataLen;     //< The number of bytes of data stored in this record.
        public int type;        //< The Intel HEX8 record type of this record.
        public byte checksum;   //< The checksum of this record.
    }

    /// <summary>
    /// IntelHex is the base class to work with Intel Hex records.
    /// This class will contain all necessary functions to process data using the Intel Hex record standard.
    /// </summary>
    public class IntelHex
    {
        // 768 should be plenty of space to read in a Intel HEX8 record
        const int IHEX_RECORD_BUFF_SIZE = 768;
        // Offsets and lengths of various fields in an Intel HEX8 record
        const int IHEX_COUNT_OFFSET = 1;
        const int IHEX_COUNT_LEN = 2;
        const int IHEX_ADDRESS_OFFSET = 3;
        const int IHEX_ADDRESS_LEN = 4;
        const int IHEX_TYPE_OFFSET = 7;
        const int IHEX_TYPE_LEN = 2;
        const int IHEX_DATA_OFFSET = 9;
        const int IHEX_CHECKSUM_LEN = 2;
        public const int IHEX_MAX_DATA_LEN = 512;
        // Ascii hex encoded length of a single byte
        const int IHEX_ASCII_HEX_BYTE_LEN = 2;
        // Start code offset and value
        const int IHEX_START_CODE_OFFSET = 0;
        const char IHEX_START_CODE = ':';

        const int IHEX_OK = 0; 				            //< Error code for success or no error.
        const int IHEX_ERROR_FILE = -1; 			    //< Error code for error while reading from or writing to a file. You may check errno for the exact error if this error code is encountered.
        const int IHEX_ERROR_EOF = -2; 			        //< Error code for encountering end-of-file when reading from a file.
        const int IHEX_ERROR_INVALID_RECORD = -3; 	    //< Error code for error if an invalid record was read.
        const int IHEX_ERROR_INVALID_ARGUMENTS = -4; 	//< Error code for error from invalid arguments passed to function.
        const int IHEX_ERROR_NEWLINE = -5;		        //< Error code for encountering a newline with no record when reading from a file.
        const int IHEX_ERROR_INVALID_STRUCTURE = -6;    //< Error code for not building a structure prior to calling the function.

        const int IHEX_TYPE_00 = 0;                     //< Data Record
        const int IHEX_TYPE_01 = 1;                     //< End of File Record
        const int IHEX_TYPE_02 = 2;                     //< Extended Segment Address Record
        const int IHEX_TYPE_03 = 3;                     //< Start Segment Address Record
        const int IHEX_TYPE_04 = 4;                     //< Extended Linear Address Record
        const int IHEX_TYPE_05 = 5;                     //< Start Linear Address Record
        
        int status = IHEX_ERROR_INVALID_ARGUMENTS;          // internal variable that saves the status of the last function call.

        // Accessor variable to return status of last function call.
        public int Status
        {
            get { return status; }
        }

        /// <summary>
        /// Initializes a new IntelHex structure that is returned upon successful completion of the function,
        /// including up to 16-bit integer address, 8-bit data array, and size of 8-bit data array.
        /// </summary>
        /// <param name="type">The type of Intel HEX record to be defined by the record.</param>
        /// <param name="address">The 16-, 24-, or 32-bit address of the record.</param>
        /// <param name="data">An array of 8-bit data bytes.</param>
        /// <param name="dataLen">The number of data bytes passed in the array.</param>
        /// <returns>IntelHexStructure instance or null, if null then query Status class variable for the error.</returns>
        public IntelHexStructure NewRecord(int type, UInt16 address, byte[] data, int dataLen)
        {
            IntelHexStructure result = new IntelHexStructure();
            // Data length size check, assertion of irec pointer
            if (dataLen < 0 || dataLen > IHEX_MAX_DATA_LEN / 2)
            {
                status = IHEX_ERROR_INVALID_ARGUMENTS;
                return null;
            }

            result.type = type;
            result.address = address;
            if (data != null)
                Array.Copy(data, result.data, (long)dataLen);
            result.dataLen = dataLen;
            result.checksum = CalcChecksum(result);

            status = IHEX_OK;
            return result;
        }

        /// <summary>
        /// Utility function to read an Intel HEX8 record from a file
        /// </summary>
        /// <param name="stringLineFromFile">A line of string from Intel Hex File.</param>
        /// <returns>IntelHexStructure instance or null, if null then query Status class variable for the error.</returns>
        public IntelHexStructure ConvertFromString(string stringLineFromFile)
        {
            int dataCount, i;

            IntelHexStructure result = new IntelHexStructure();

            // Check our the String
            if (string.IsNullOrEmpty(stringLineFromFile))
            {
                status = IHEX_ERROR_NEWLINE;
                return null;
            }
            
            // Size check for start code, count, address, and type fields
            if (stringLineFromFile.Length < (1 + IHEX_COUNT_LEN + IHEX_ADDRESS_LEN + IHEX_TYPE_LEN))
            {
                status = IHEX_ERROR_INVALID_RECORD;
                return null;
            }
            
            // Check the for colon start code
            if (stringLineFromFile[IHEX_START_CODE_OFFSET] != IHEX_START_CODE)
            {
                status = IHEX_ERROR_INVALID_RECORD;
                return null;
            }

            // Copy the ASCII hex encoding of the count field into hexBuff, convert it to a usable integer
            dataCount = Convert.ToInt16(stringLineFromFile.Substring(IHEX_COUNT_OFFSET, IHEX_COUNT_LEN), 16);

            // Copy the ASCII hex encoding of the address field into hexBuff, convert it to a usable integer
            result.address = Convert.ToUInt16(stringLineFromFile.Substring(IHEX_ADDRESS_OFFSET, IHEX_ADDRESS_LEN), 16);

            // Copy the ASCII hex encoding of the address field into hexBuff, convert it to a usable integer
            result.type = Convert.ToInt16(stringLineFromFile.Substring(IHEX_TYPE_OFFSET, IHEX_TYPE_LEN), 16);

            // Size check for start code, count, address, type, data and checksum fields
            if (stringLineFromFile.Length < (1 + IHEX_COUNT_LEN + IHEX_ADDRESS_LEN + IHEX_TYPE_LEN + dataCount * 2 + IHEX_CHECKSUM_LEN))
            {
                status = IHEX_ERROR_INVALID_RECORD;
                return null;
            }

            // Loop through each ASCII hex byte of the data field, pull it out into hexBuff,
            // convert it and store the result in the data buffer of the Intel HEX8 record
            for (i = 0; i < dataCount; i++)
            {
                // Times two i because every byte is represented by two ASCII hex characters
                result.data[i] = Convert.ToByte(stringLineFromFile.Substring(IHEX_DATA_OFFSET + 2 * i, IHEX_ASCII_HEX_BYTE_LEN), 16);
            }
            result.dataLen = dataCount;

            // Copy the ASCII hex encoding of the checksum field into hexBuff, convert it to a usable integer
            result.checksum = Convert.ToByte(stringLineFromFile.Substring(IHEX_DATA_OFFSET + dataCount * 2, IHEX_CHECKSUM_LEN), 16);

            if (result.checksum != CalcChecksum(result))
            {
                status = IHEX_ERROR_INVALID_RECORD;
                return null;
            }

            status = IHEX_OK;
            return result;
        }


        // Utility function to convert IntelHexStructure to a line of string
        public string ConvertToString(IntelHexStructure hexStructure)
        {
            StringBuilder sb = new StringBuilder();

            // Check that the data length is in range
            if (hexStructure.dataLen > IHEX_MAX_DATA_LEN / 2)
            {
                status = IHEX_ERROR_INVALID_RECORD;
                return null;
            }

            try
            {
                // Write the start code, data count, address, and type fields
                sb.AppendFormat("{0}{1:X2}{2:X4}{3:X2}", IHEX_START_CODE, hexStructure.dataLen, hexStructure.address, hexStructure.type);

                // Write the data bytes
                for (int i = 0; i < hexStructure.dataLen; i++)
                    sb.AppendFormat("{0:X2}", hexStructure.data[i]);
                // Last but not least, the checksum
                sb.AppendFormat(String.Format("{0:X2}/n", CalcChecksum(hexStructure)));
            }
            catch (Exception)
            {
                status = IHEX_ERROR_FILE;
                return null;
            }

            status = IHEX_OK;
            return sb.ToString();
        }

        /// <summary>
        /// Utility function to print the information stored in an Intel HEX8 record
        /// </summary>
        /// <param name="verbose">A boolean set to false by default, if set to true will provide extended information.</param>
        /// <returns>String which provides the output of the function, this does not write directly to the console.</returns>
        public String Print(IntelHexStructure intelHexStructure, bool verbose = false)
        {
            int i;
            String returnString;

            if (verbose)
            {
                returnString = String.Format("Intel HEX8 Record Type: \t{0}\n", intelHexStructure.type);
                returnString += String.Format("Intel HEX8 Record Address: \t0x{0:X4}\n", intelHexStructure.address);
                returnString += String.Format("Intel HEX8 Record Data: \t[");
                for (i = 0; i < intelHexStructure.dataLen; i++)
                {
                    if (i + 1 < intelHexStructure.dataLen)
                        returnString += String.Format("0x{0:X02}, ", intelHexStructure.data[i]);
                    else
                        returnString += String.Format("0x{0:X02}", intelHexStructure.data[i]);
                }
                returnString += String.Format("]\n");
                returnString += String.Format("Intel HEX8 Record CalcChecksum: \t0x{0:X2}\n", intelHexStructure.checksum);
            }
            else
            {
                returnString = String.Format("{0}{1:X2}{2:X4}{3:X2}", IHEX_START_CODE, intelHexStructure.dataLen, intelHexStructure.address, intelHexStructure.type);
                for (i = 0; i < intelHexStructure.dataLen; i++)
                    returnString += String.Format("{0:X2}", intelHexStructure.data[i]);
                returnString += String.Format("{0:X2}", CalcChecksum(intelHexStructure));
            }
            status = IHEX_OK;
            return (returnString);
        }

        /// <summary>
        /// An internal utility function to calculate the checksum of an Intel HEX8 record
        /// </summary>
        /// <returns>byte which is the checksum of IntelHexStructure.</returns>
        internal static byte CalcChecksum(IntelHexStructure hexStructure)
        {
            byte checksum;
            int i;

            // Add the data count, type, address, and data bytes together
            checksum = (byte)hexStructure.dataLen;
            checksum += (byte)hexStructure.type;
            checksum += (byte)hexStructure.address;
            checksum += (byte)((hexStructure.address & 0xFF00) >> 8);
            for (i = 0; i < hexStructure.dataLen; i++)
                checksum += hexStructure.data[i];

            // Two's complement on checksum
            checksum = (byte)(~checksum + 1);

            return checksum;
        }
    }
}
