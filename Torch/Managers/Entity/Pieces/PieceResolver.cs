using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Torch.Managers.Entity.Pieces
{
    public static class PieceResolver
    {
        private static readonly Dictionary<string, KeyValuePair<Type, PieceAttribute>> _registeredQueryTypes =
            new Dictionary<string, KeyValuePair<Type, PieceAttribute>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> _aliases = new Dictionary<string, string>();

        private static readonly List<PieceAttribute> _pieceAttributes = new List<PieceAttribute>();
        private static readonly List<PieceAliasAttribute> _aliasAttributes = new List<PieceAliasAttribute>();

        private static void Register(Type t)
        {
            var attr = new KeyValuePair<Type, PieceAttribute>(t, t.GetCustomAttribute<PieceAttribute>());
            _pieceAttributes.Add(attr.Value);
            foreach (string k in attr.Value.TagNames)
            {
                if (_aliases.ContainsKey(k))
                    throw new Exception($"Alias already exists for {k}");
                _registeredQueryTypes.Add(k.ToLower(), attr);
            }

            foreach (PieceAliasAttribute k in t.GetCustomAttributes<PieceAliasAttribute>())
            {
                _aliasAttributes.Add(k);
                foreach (string a in k.Aliases)
                {
                    if (_registeredQueryTypes.ContainsKey(a))
                        throw new Exception($"Query piece already exists");
                    _aliases.Add(a, k.Command);
                }
            }
        }

        public static bool TryGetAlias(string key, out string result)
        {
            return _aliases.TryGetValue(key, out result);
        }

        public static bool TryGetPiece(string key, out Type type, out PieceAttribute attr)
        {
            if (_registeredQueryTypes.TryGetValue(key, out KeyValuePair<Type, PieceAttribute> res))
            {
                type = res.Key;
                attr = res.Value;
                return true;
            }

            type = null;
            attr = null;
            return false;
        }

        static PieceResolver()
        {
            foreach (Type type in typeof(PieceResolver).Assembly.GetTypes())
                if (type.HasAttribute<PieceAttribute>())
                    Register(type);
        }

        public static string GenerateDocumentation()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Format:");
            sb.AppendLine("\tfilter and (filterWithValue:value or filterWithValue:'value with bangs!')");
            sb.AppendLine();
            sb.AppendLine("Filters:");
            foreach (PieceAttribute k in _pieceAttributes)
            {
                sb.Append("\t").AppendLine(string.Join(", ", k.TagNames.Select(x=>"\""+x+"\"")));
                sb.Append("\t\tRequires Value: ").AppendLine(k.RequiresValue ? "yes" : "no");
                if (k.Examples.Count > 0)
                    sb.AppendLine("\t\tExamples:");
                foreach (string ex in k.Examples)
                    sb.Append("\t\t\t").AppendLine(ex);
            }

            sb.AppendLine();
            sb.AppendLine("Aliases:");
            foreach (PieceAliasAttribute a in _aliasAttributes)
            {
                sb.Append("\t\"").Append(a.Command).Append("\" as ")
                    .AppendLine(string.Join(", ", a.Aliases.Select(x => "\"" + x + "\"")));
            }

            sb.AppendLine();
            sb.AppendLine("Examples:");
            sb.AppendLine("\tblock.owner:'Equinox' and block.def:'Refinery/LargeRefinery'");
            sb.AppendLine("\tstatic or (moving and dynamic)");

            return sb.ToString();
        }
    }
}