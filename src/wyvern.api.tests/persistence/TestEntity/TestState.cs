using System;
using System.Collections.Generic;
using System.Linq;
using wyvern.entity.state;

namespace wyvern.api.tests
{
    public class TestState : AbstractState
    {
        public TestEntity.Mode Mode { get; }
        public IEnumerable<string> Words { get; }

        public TestState(TestEntity.Mode mode, IEnumerable<string> words)
        {
            Mode = mode;
            Words = words.ToArray();
        }

        public TestState WithMode(TestEntity.Mode mode)
        {
            return new TestState(mode, Words);
        }

        public TestState AddText(string word)
        {
            var words = Words.ToArray();
            var w = new string[words.Length + 1];

            if (Mode == TestEntity.Mode.APPEND)
            {
                for (var i = 0; i < words.Length; i++)
                    w[i] = words[i];
                w[w.Length - 1] = word;
            }
            else if (Mode == TestEntity.Mode.PREPEND)
            {
                w[0] = word;
                for (var i = 0; i < words.Length; i++)
                    w[i + 1] = words[i];
            }

            return new TestState(Mode, w);
        }
    }
}