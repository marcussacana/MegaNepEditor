using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MegaNepEditor
{
    internal class ExtHelper
    {
        public static string GetExtension(int Signature)
        {
            string Result = "";
            for (int i = 0; i < 32; i += 8)
            {
                byte Char = (byte)(Signature >> i);
                if ((Char >= 'A' && Char <= 'Z') || (Char >= 'a' && Char <= 'z') || (Char >= '0' && Char <= '9'))
                {
                    Result += (char)Char;
                }
                else break;
            }

            if (Result.Length < 3)
                return ".bin";

            return "." + Result.ToLowerInvariant();
        }
    }
}
