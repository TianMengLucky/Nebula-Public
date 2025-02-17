global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;

using HarmonyLib;

namespace AddonDev;

public class AddonLib
{
    public static Harmony AddonHarmony = new("meng.tian.addon.nebula");
}