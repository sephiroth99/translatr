using System;
using System.IO;

namespace translatr
{
    public enum LocaleID
    {
        Default = -1,
        English = 0,
        French,
        German,
        Italian,
        Spanish,
        Japanese,
        Portugese,
        Polish,
        EnglishUK,
        Russian,
        Czech,
        Dutch,
        Hungarian
    };

    class Locale
    {
        static public uint getLocaleMask(string dir)
        {
            uint mask = uint.MaxValue;

            // Get all directories containing locals.bin
            var dirs = Directory.GetFiles(dir, "locals.bin", SearchOption.AllDirectories);

            if (dirs.Length > 1)
            {
                foreach (string d in dirs)
                {
                    var locale = d.Substring(dir.Length + 1, 8);

                    if (locale == "default\\")
                    {
                        // We shouldnt have default!
                        throw new NotSupportedException();
                    }
                    else
                    {
                        mask &= uint.Parse(locale, System.Globalization.NumberStyles.HexNumber);
                    }
                }
                mask = ~mask;
            }
            else if (dirs.Length == 1)
            {
                var locale = dirs[0].Substring(dir.Length + 1, 8);
                if (locale == "default\\")
                    mask = uint.MaxValue - 1;
                else
                    mask = uint.MaxValue;
            }
            else
            {
                mask = uint.MaxValue;
            }

            return mask;
        }

        public static string toString(LocaleID id)
        {
            switch (id)
            {
                case LocaleID.English:
                    return "English";


                case LocaleID.French:
                    return "French";


                case LocaleID.German:
                    return "German";


                case LocaleID.Italian:
                    return "Italian";


                case LocaleID.Spanish:
                    return "Spanish";


                case LocaleID.Japanese:
                    return "Japanese";


                case LocaleID.Portugese:
                    return "Portugese";


                case LocaleID.Polish:
                    return "Polish";


                case LocaleID.EnglishUK:
                    return "EnglishUK";


                case LocaleID.Russian:
                    return "Russian";


                case LocaleID.Czech:
                    return "Czech";


                case LocaleID.Dutch:
                    return "Dutch";

                case LocaleID.Hungarian:
                    return "Hungarian";

                default:
                    return "Default";
            }
        }

        public static LocaleID getFromString(String s)
        {
            LocaleID id = LocaleID.Default;

            switch (s)
            {
                case "English":
                    id = LocaleID.English;
                    break;

                case "French":
                    id = LocaleID.French;
                    break;

                case "German":
                    id = LocaleID.German;
                    break;

                case "Italian":
                    id = LocaleID.Italian;
                    break;

                case "Spanish":
                    id = LocaleID.Spanish;
                    break;

                case "Japanese":
                    id = LocaleID.Japanese;
                    break;

                case "Portugese":
                    id = LocaleID.Portugese;
                    break;

                case "Polish":
                    id = LocaleID.Polish;
                    break;

                case "EnglishUK":
                    id = LocaleID.EnglishUK;
                    break;

                case "Russian":
                    id = LocaleID.Russian;
                    break;

                case "Czech":
                    id = LocaleID.Czech;
                    break;

                case "Dutch":
                    id = LocaleID.Dutch;
                    break;

                case "Hungarian":
                    id = LocaleID.Hungarian;
                    break;
            }

            return id;
        }
    }
}
