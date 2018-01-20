using System;
using System.Collections.Generic;
using Sandbox.Game.Entities.Cube;
using Torch.API;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PieceAttribute : Attribute
    {
        public IReadOnlyList<string> TagNames { get; }

        public bool RequiresValue { get; set; } = true;

        public IReadOnlyList<string> Examples { get; private set; } = new List<string>();

        public string ExampleParse
        {
            get { return Examples == null ? null : string.Join("\n", Examples); }
            set { Examples = value.Split('\n'); }
        }

        public PieceAttribute(params string[] tagNames)
        {
            TagNames = tagNames;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PieceAliasAttribute : Attribute
    {
        public string[] Aliases { get; }
        public string Command { get; }

        public PieceAliasAttribute(string alias, string command)
        {
            Aliases = new []{alias};
            Command = command;
        }

        public PieceAliasAttribute(string[] alias, string command)
        {
            Aliases = alias;
            Command = command;
        }
    }

    public abstract class Piece
    {
        protected ITorchBase Torch { get; }

        protected Piece(ITorchBase torch)
        {
            Torch = torch;
        }

        /// <summary>
        /// Does the given object match the query.
        /// </summary>
        /// <param name="e">object to test</param>
        /// <returns>true</returns>
        public abstract bool Test(object e);

        /// <summary>
        /// Does the entity require testing its children individually.
        /// </summary>
        /// <param name="entity">entity to test</param>
        /// <returns>null if it cannot be determined, otherwise the result</returns>
        public virtual bool ChildrenRelevant(MyEntity entity)
        {
            return true;
        }


        /// <summary>
        /// Can the given object, or children of the given object, be tested.
        /// </summary>
        /// <param name="ent">Object to evaluate</param>
        /// <returns>true if we can test the given object</returns>
        public virtual bool CanTest(object ent)
        {
            return true;
        }

        protected static bool GlobbedEquals(string needle, string haystack)
        {
            if (needle.Equals("*"))
                return true;
            bool gs = needle.StartsWith("*");
            bool ge = needle.EndsWith("*");
            if (gs && ge)
                return haystack.Contains(needle.Substring(1, needle.Length - 2), StringComparison.OrdinalIgnoreCase);
            if (gs)
                return haystack.EndsWith(needle.Substring(1), StringComparison.OrdinalIgnoreCase);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (ge)
                return haystack.StartsWith(needle.Substring(0, needle.Length - 1), StringComparison.OrdinalIgnoreCase);
            return haystack.Equals(needle, StringComparison.OrdinalIgnoreCase);
        }
    }
}