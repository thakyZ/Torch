using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;
using VRage.Generics;
using VRageMath;

namespace Torch.Managers.Entity
{
    public class EntityQuery : IEnumerable<object>, IEnumerable<MySlimBlock>, IEnumerable<MyEntity>
    {
        public EntityManager Manager { get; }
        private readonly Pieces.Piece _query;

        public EntityQuery(EntityManager mgr, string query)
        {
            Manager = mgr;
            _query = QueryParser.Parse(mgr.Torch, query);
        }

        IEnumerator<MyEntity> IEnumerable<MyEntity>.GetEnumerator() => Entities();

        IEnumerator<MySlimBlock> IEnumerable<MySlimBlock>.GetEnumerator() => Blocks();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => Everything();

        IEnumerator IEnumerable.GetEnumerator() => Everything();

        private static readonly MyConcurrentObjectsPool<List<MyEntity>> _allEntities =
            new MyConcurrentObjectsPool<List<MyEntity>>(1);

        private static readonly MyConcurrentObjectsPool<List<MySlimBlock>> _slimCache =
            new MyConcurrentObjectsPool<List<MySlimBlock>>(1);

        private void CopyInGameThread<T>(List<T> dest, Func<HashSet<T>> source)
        {
            dest.Clear();
            int sizeEarly = MathHelper.GetNearestBiggerPowerOfTwo(MyEntities.GetEntities().Count);
            if (dest.Capacity < sizeEarly || dest.Capacity * 8 > sizeEarly)
                dest.Capacity = sizeEarly;

            Manager.Torch.InvokeBlocking(() =>
            {
                HashSet<T> es = source();
                int sizeFinal = MathHelper.GetNearestBiggerPowerOfTwo(es.Count);
                if (dest.Capacity < sizeFinal || dest.Capacity * 8 > sizeFinal)
                    dest.Capacity = sizeFinal;
                T[] dar = dest.GetInternalArray();
                int count = Math.Min(dar.Length, es.Count);
                es.CopyTo(dar, 0, count);
                dest.SetSize(count);
            });
        }

        /// <summary>
        /// Everything that matches this query.  Either MyEntity or MySlimBlock.  Returns the slim block for all blocks.
        /// </summary>
        /// <returns>everything matching this</returns>
        public IEnumerator<object> Everything()
        {
            _allEntities.AllocateOrCreate(out List<MyEntity> topMost);
            try
            {
                CopyInGameThread(topMost, MyEntities.GetEntities);
                foreach (MyEntity ent in topMost)
                {
                    if (ent is MyCubeBlock block)
                    {
                        if (_query.CanTest(ent) && _query.Test(ent))
                            yield return block.SlimBlock;
                        else if (_query.CanTest(block.SlimBlock) && _query.Test(block.SlimBlock))
                            yield return block.SlimBlock;
                    }
                    else if (_query.CanTest(ent) && _query.Test(ent))
                        yield return ent;

                    if (!_query.ChildrenRelevant(ent))
                        continue;

                    if (ent is MyCubeGrid grid)
                    {
                        _slimCache.AllocateOrCreate(out List<MySlimBlock> slimBlocks);
                        try
                        {
                            CopyInGameThread(slimBlocks, grid.GetBlocks);
                            foreach (MySlimBlock slim in slimBlocks)
                            {
                                // Handled by GetEntities
                                if (slim.FatBlock != null)
                                    continue;
                                if (!_query.CanTest(slim) || !_query.Test(slim))
                                    continue;
                                yield return slim;
                            }
                        }
                        finally
                        {
                            slimBlocks.Clear();
                            _slimCache.Deallocate(slimBlocks);
                        }
                    }
                }
            }
            finally
            {
                topMost.Clear();
                _allEntities.Deallocate(topMost);
            }
        }

        /// <summary>
        /// All slim blocks that match this query.
        /// </summary>
        /// <returns>the blocks</returns>
        public IEnumerator<MySlimBlock> Blocks()
        {
            _allEntities.AllocateOrCreate(out List<MyEntity> topMost);
            try
            {
                CopyInGameThread(topMost, MyEntities.GetEntities);
                foreach (MyEntity ent in topMost)
                {
                    if (ent is MyCubeBlock block)
                    {
                        if (_query.CanTest(ent) && _query.Test(ent))
                            yield return block.SlimBlock;
                        else if (_query.CanTest(block.SlimBlock) && _query.Test(block.SlimBlock))
                            yield return block.SlimBlock;
                    }

                    if (!_query.ChildrenRelevant(ent))
                        continue;

                    if (ent is MyCubeGrid grid)
                    {
                        _slimCache.AllocateOrCreate(out List<MySlimBlock> slimBlocks);
                        try
                        {
                            CopyInGameThread(slimBlocks, grid.GetBlocks);
                            foreach (MySlimBlock slim in slimBlocks)
                            {
                                // Handled by GetEntities
                                if (slim.FatBlock != null)
                                    continue;
                                if (!_query.CanTest(slim) || !_query.Test(slim))
                                    continue;
                                yield return slim;
                            }
                        }
                        finally
                        {
                            slimBlocks.Clear();
                            _slimCache.Deallocate(slimBlocks);
                        }
                    }
                }
            }
            finally
            {
                topMost.Clear();
                _allEntities.Deallocate(topMost);
            }
        }

        /// <summary>
        /// All entities that match this query (including fat blocks)
        /// </summary>
        /// <returns>the entities</returns>
        public IEnumerator<MyEntity> Entities()
        {
            _allEntities.AllocateOrCreate(out List<MyEntity> topMost);
            try
            {
                CopyInGameThread(topMost, MyEntities.GetEntities);
                foreach (MyEntity ent in topMost)
                {
                    if (_query.CanTest(ent) && _query.Test(ent))
                        yield return ent;
                    else if (ent is MyCubeBlock block && _query.CanTest(block.SlimBlock) &&
                             _query.Test(block.SlimBlock))
                        yield return ent;
                }
            }
            finally
            {
                topMost.Clear();
                _allEntities.Deallocate(topMost);
            }
        }
    }
}