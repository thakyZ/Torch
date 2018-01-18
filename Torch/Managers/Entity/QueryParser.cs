using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.API;
using Torch.Managers.Entity.Pieces;

namespace Torch.Managers.Entity
{
    public static class QueryParser
    {
        public class ParseException : Exception
        {
            public ParseException(string msg) : base(msg)
            {
            }
        }

        public static Piece Parse(ITorchBase torch, string query)
        {
            using (var e = new QueryParser.PeekableEnumerator<string>(Tokenize(query).GetEnumerator()))
            {
                return ParseBlock(e, torch, false);
            }
        }

        private static Piece ParseBlock(PeekableEnumerator<string> tokens, ITorchBase torch, bool once)
        {
            Piece result = null;

            while (true)
            {
                string tok = tokens.Peek;
                if (tok == null)
                    break;
                Console.WriteLine(tok);
                if (tok == "(")
                {
                    if (result != null)
                        throw new ParseException("Query blocks must be separated by logic operators");
                    tokens.Next();
                    result = ParseBlock(tokens, torch, false);
                    continue;
                }

                if (tok == ")")
                {
                    tokens.Next();
                    break;
                }

                if (tok.Equals("and", StringComparison.OrdinalIgnoreCase))
                {
                    if (result == null)
                        throw new ParseException("Logic operator must be preceded by a query block");
                    tokens.Next();
                    result = new EntityQueryComposite(torch, EntityQueryComposite.LogicOp.And,
                        new[] {result, ParseBlock(tokens, torch, true)});
                    continue;
                }

                if (tok.Equals("or", StringComparison.OrdinalIgnoreCase))
                {
                    if (result == null)
                        throw new ParseException("Logic operator must be preceded by a query block");
                    tokens.Next();
                    result = new EntityQueryComposite(torch, EntityQueryComposite.LogicOp.Or,
                        new[] {result, ParseBlock(tokens, torch, false)});
                    continue;
                }

                result = ParseOne(tokens, torch);
                if (once)
                    return result;
            }

            if (result == null)
                throw new ParseException("Empty block!");
            return result;
        }

        private static Piece ParseOne(PeekableEnumerator<string> tokens, ITorchBase torch)
        {
            string tag = tokens.Next();
            if (PieceResolver.TryGetAlias(tag, out string command))
            {
                if (command.Contains("{0}"))
                {
                    tokens.Next(); // :
                    string value = tokens.Next();
                    if (value == null)
                        throw new ParseException($"Alias {tag} requires a value but one wasn't supplied");

                    command = string.Format(command, value);
                }
                return Parse(torch, command);
            }

            if (!PieceResolver.TryGetPiece(tag, out Type type, out PieceAttribute attr))
                throw new ParseException($"Unknown tag {tag}");

            if ((tokens.Peek == ":") != attr.RequiresValue)
            {
                string a = attr.RequiresValue
                    ? "requires a value but one wasn't supplied"
                    : "doesn't require a value but one was supplied";
                throw new ParseException($"Tag {tag} {a}");
            }

            if (attr.RequiresValue)
            {
                tokens.Next(); // :
                string value = tokens.Next();
                if (value == null)
                    throw new ParseException($"Tag {tag} requires a value but one wasn't supplied");

                return (Piece) Activator.CreateInstance(type, torch, value);
            }

            return (Piece) Activator.CreateInstance(type, torch);
        }

        private static IEnumerable<string> Tokenize(string query)
        {
            var escaped = false;
            var quoted = false;
            var token = new StringBuilder();
            var parenLevel = 0;
            var head = 0;
            while (head < query.Length)
            {
                char c = query[head++];
                if (!escaped && c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (escaped)
                {
                    escaped = false;
                    switch (c)
                    {
                        case 't':
                            token.Append('\t');
                            break;
                        case 'n':
                            token.Append('\n');
                            break;
                        default:
                            token.Append(c);
                            break;
                    }

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    if (token.Length > 0)
                        yield return token.ToString();
                    token.Clear();
                    quoted = !quoted;
                    continue;
                }

                if (quoted)
                {
                    token.Append(c);
                    continue;
                }

                string control = null;
                switch (c)
                {
                    case '(':
                        control = "(";
                        parenLevel++;
                        break;
                    case ')':
                        if (parenLevel <= 0)
                            throw new QueryParser.ParseException("No matching paren");
                        control = ")";
                        parenLevel--;
                        break;
                    case ':':
                        control = ":";
                        break;
                    default:
                        if (!char.IsWhiteSpace(c))
                        {
                            token.Append(c);
                            continue;
                        }

                        break;
                }

                if (token.Length > 0)
                {
                    yield return token.ToString();
                    token.Clear();
                }

                if (control != null)
                    yield return control;
            }

            if (parenLevel > 0)
                throw new ParseException("Dangling paren");
            if (escaped)
                throw new ParseException("Dangling backslash");
            if (quoted)
                throw new ParseException("Unclosed quote");
        }

        private class PeekableEnumerator<T> : IDisposable where T : class
        {
            private readonly IEnumerator<T> _backing;
            private T _peeked;

            public PeekableEnumerator(IEnumerator<T> backing)
            {
                _backing = backing;
                _peeked = null;
            }

            public void Dispose()
            {
                _backing.Dispose();
            }

            public T Peek
            {
                get
                {
                    if (_peeked == null && _backing.MoveNext())
                        _peeked = _backing.Current;
                    return _peeked;
                }
            }

            public T Next()
            {
                T res = Peek;
                _peeked = null;
                return res;
            }
        }
    }
}