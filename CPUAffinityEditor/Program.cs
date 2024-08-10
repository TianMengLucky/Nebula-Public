using System;
using System.Diagnostics;

namespace CPUAffinityEditor
{
    class FloatWithIndex
    {
        public int index { get; private set; }
        public float value { get; set; }

        public FloatWithIndex(int index)
        {
            this.index = index;
            value = 0.0f;
        }

        public FloatWithIndex(int index,float value)
        {
            this.index = index;
            this.value = value;
        }
    }
    class Program
    {
        static FloatWithIndex[] GetProcessorBusyness(float duration)
        {
            var processors = Environment.ProcessorCount;
            var counters = new PerformanceCounter[processors];
            var result = new FloatWithIndex[processors];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new FloatWithIndex(i);
                counters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
            }
            
            var max = (int)(duration * 100);
            for(var t = 0; t < max; t++)
            {
                for (var i = 0; i < result.Length; i++)
                {
                    result[i].value += counters[i].NextValue();
                }
                System.Threading.Thread.Sleep(10);
            }

            //計測時間に依らず平均使用率を返すようにする
            for (var i = 0; i < result.Length; i++)
            {
                result[i].value /= duration;
            }

            return result;
        }

        static FloatWithIndex[] GetPhysicalProcessorBusyness(FloatWithIndex[] logicalBusyness)
        {
            var result = new FloatWithIndex[logicalBusyness.Length / 2];
            for(var i = 0; i < result.Length; i++)
            {
                result[i] = new FloatWithIndex(i, (logicalBusyness[i * 2].value + logicalBusyness[i * 2 + 1].value) * 0.5f);
            }
            return result;
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 2) return;

                //プロセスを取得
                var p = Process.GetProcessById(int.Parse(args[0]));


                //優先度の設定

                if (args[1] == "Enable")
                {
                    p.PriorityBoostEnabled = true;
                    return;

                }
                else if(args[1]=="Disable")
                {
                    p.PriorityBoostEnabled = false;
                    return;
                }

                //Affinityの設定

                if (args[1] == "0")
                {
                    //全てのコアを使用可能にする(Dont Care)
                    var processors=Environment.ProcessorCount;
                    var affinity = 0;
                    for(var i = 0; i < processors; i++)
                    {
                        affinity <<= 1;
                        affinity |= 1;
                    }
                    p.ProcessorAffinity = (IntPtr)affinity;
                    return;
                }

                var busyness = GetProcessorBusyness(1f);

                if (args[1] == "1")
                {
                    //1つのコアを選択
                    Array.Sort(busyness, (a, b) => a.value - b.value > 0 ? 1 : -1);
                    p.ProcessorAffinity = (IntPtr)(1 << busyness[0].index);
                }
                else if (args[1] == "2")
                {
                    //2つのコアを選択
                    Array.Sort(busyness, (a, b) => a.value - b.value > 0 ? 1 : -1);
                    var mask = 0;
                    for(var i = 0; i < 2; i++)
                    {
                        mask |= 1 << busyness[i].index;
                    }
                    p.ProcessorAffinity = (IntPtr)(mask);
                }
                else if (args[1] == "2HT")
                {
                    //同じ物理コアからなる2つのコアを選択(nコア2nスレッドのタイプと仮定)
                    busyness = GetPhysicalProcessorBusyness(busyness);
                    Array.Sort(busyness, (a, b) => a.value - b.value > 0 ? 1 : -1);
                    p.ProcessorAffinity = (IntPtr)(0b11 << (busyness[0].index*2));
                }
            }
            catch
            {
            }
        }
    }
}
