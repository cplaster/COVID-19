using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace COVID_19
{
    public static class Serializer
    {
        public static void Serialize(object o, string filename)
        {
            Stream s = File.OpenWrite(filename);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(s, o);
            s.Close();


        }

        public static object Deserialize(string filename)
        {
            object o = null;

            FileInfo fi = new FileInfo(filename);
            if (fi.Exists && fi.Length > 0)
            {
                Stream s = File.OpenRead(filename);
                BinaryFormatter b = new BinaryFormatter();
                o = b.Deserialize(s);
                s.Close();
            }

            return o;
        }
    }
}
