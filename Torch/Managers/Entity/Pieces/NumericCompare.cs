using System;
using System.Collections.Generic;
using System.Reflection;
using Sandbox.Game.Entities.Cube;
using Torch.API;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    public abstract class NumericCompare : Piece
    {
        private class ComparatorAttribute : Attribute
        {
            public IReadOnlyList<string> Keys { get; }

            public ComparatorAttribute(params string[] keys)
            {
                Keys = keys;
            }
        }

        private enum Comparator
        {
            [Comparator("=", "==")]
            Equal,

            [Comparator("!=")]
            NotEqual,

            [Comparator(">")]
            Greater,

            [Comparator("<")]
            Lesser,

            [Comparator(">=", "=>")]
            GreaterOrEqual,

            [Comparator("<=", "=<")]
            LessOrEqual
        }

        private static readonly Dictionary<string, Comparator> _comparatorByKey;
        private static readonly Dictionary<Comparator, string> _keyByComparator;

        static NumericCompare()
        {
            _comparatorByKey = new Dictionary<string, Comparator>();
            _keyByComparator = new Dictionary<Comparator, string>();
            foreach (FieldInfo f in typeof(Comparator).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var attr = f.GetCustomAttribute<ComparatorAttribute>();
                var c = (Comparator) f.GetValue(null);
                foreach (string k in attr.Keys)
                    _comparatorByKey.Add(k, c);
                _keyByComparator.Add(c, attr.Keys[0]);
            }
        }

        private readonly Comparator _comparer;
        private readonly double _against;

        protected NumericCompare(ITorchBase torch, string value) : base(torch)
        {
            value = value.TrimStart();

            int i = Math.Min(2, value.Length);
            _comparer = Comparator.Equal;
            while (i >= 1)
            {
                if (_comparatorByKey.TryGetValue(value.Substring(0, i), out Comparator tmp))
                {
                    _comparer = tmp;
                    break;
                }

                i--;
            }

            _against = double.Parse(value.Substring(i));
        }

        protected abstract double? Get(MySlimBlock block);
        protected abstract double? Get(MyEntity entity);

        public override bool Test(MySlimBlock block)
        {
            double? c = Get(block);
            return c.HasValue && Test(c.Value);
        }

        public override bool Test(MyEntity entity)
        {
            double? c = Get(entity);
            return c.HasValue && Test(c.Value);
        }

        private bool Test(double v)
        {
            switch (_comparer)
            {
                case Comparator.Equal:
                    return Math.Abs(v - _against) < Math.Max(0.0000001f, Math.Abs(v + _against) / 1e4f);
                case Comparator.NotEqual:
                    return Math.Abs(v - _against) > Math.Max(0.0000001f, Math.Abs(v + _against) / 1e4f);
                case Comparator.Greater:
                    return v > _against;
                case Comparator.Lesser:
                    return v < _against;
                case Comparator.GreaterOrEqual:
                    return v >= _against;
                case Comparator.LessOrEqual:
                    return v <= _against;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name} {_keyByComparator[_comparer]} {_against}";
        }
    }
}