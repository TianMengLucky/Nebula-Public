﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Reflection;

namespace Nebula.Language
{
    public class Language
    {
        static private Language language;

        public Dictionary<string, string> languageSet;

        public static string GetString(string key)
        {
            if (language.languageSet.ContainsKey(key))
            {
                return language.languageSet[key];
            }
            return "*" + key;
        }

        public Language()
        {
            languageSet = new Dictionary<string, string>();
        }

        public static void Load()
        {
            language = new Language();

            language.deserialize(GetDefaultLanguageStream());
            Dictionary<string, string> defaultSet = language.languageSet;
            language.deserialize(@"language\lang.dat");

            //翻訳セットに不足データがある場合デフォルト言語セットで補う
            foreach (KeyValuePair<string, string> pair in defaultSet)
            {
                if (!language.languageSet.ContainsKey(pair.Key))
                {
                    language.languageSet.Add(pair.Key, pair.Value);
                }
            }
        }

        public static Stream GetDefaultLanguageStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Nebula.Resources.Lang.dat");
        }

        public bool deserialize(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(
                        path, Encoding.GetEncoding("utf-8")))
                {
                    return deserialize(sr);
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool deserialize(Stream stream)
        {
            using (StreamReader sr = new StreamReader(
                    stream, Encoding.GetEncoding("utf-8")))
            {
                return deserialize(sr);
            }
        }

        public bool deserialize(StreamReader reader)
        {
            bool result = true;
            try
            {
                string data = "", line;


                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length < 3)
                    {
                        continue;
                    }
                    if (data.Equals(""))
                    {
                        data = line;
                    }
                    else
                    {
                        data += "," + line;
                    }
                }


                if (!data.Equals(""))
                {


                    JsonSerializerOptions option = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        WriteIndented = true
                    };

                    languageSet = JsonSerializer.Deserialize<Dictionary<string, string>>("{ " + data + " }", option);

                    result = true;
                }
            }
            catch (Exception e)
            {
                result = false;
            }

            return result;
        }
    }
}
