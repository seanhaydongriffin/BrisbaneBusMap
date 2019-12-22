using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using ProtoBuf;
using TransitRealtime;
using System.IO;
using System.Data.SQLite;
using System.Collections;

namespace GoogleMap
{
    class SQLite
    {
        public string route_id { get; set; }
 //       public Custom MyCustom { get; set; }

        public static void ToConsole(string sql, SQLiteConnection cnn)
        {
            string output = "";

            using (SQLiteCommand command = new SQLiteCommand(sql, cnn))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string line = "";

                        for (int i=0; i < reader.FieldCount; i++)
                        {
                            if (!line.Equals(""))

                                line = line + ",";

                            //                            var dtype = reader.GetFieldType(i);

                            line = line + reader.GetString(i);

                        }

                        if (!output.Equals(""))

                            output = output + Environment.NewLine;

                        output = output + line;
                    }
                }
            }

            Console.WriteLine(output);

        }

        public static List<string[]> ToList(string sql, SQLiteConnection cnn)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(sql, cnn))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        List<string[]> output = new List<string[]>();

                        while (reader.Read())
                        {
                            string[] line_arr = new string[reader.FieldCount];

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                line_arr[i] = reader.GetString(i);
                            }

                            output.Add(line_arr);
                        }

                        return output;
                    }
                }
            } catch (Exception e)
            {

            }

            return null;

        }

    }
}
