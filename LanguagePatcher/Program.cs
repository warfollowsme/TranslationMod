﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MultiLanguage;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LanguagePatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            LocalizationBridge.Localization = new MultiLanguage.Localization();

            var pathcedName = "Stardew Valley.exe";
            var gameAssembly = AssemblyDefinition.ReadAssembly("Stardew Valley.exe");

            var injectees = gameAssembly.Modules.SelectMany(mod => ModuleDefinitionRocks.GetAllTypes(mod))
                                                .SelectMany(t => t.Methods)
                                                .Where(method => null != method.Body).ToList();

            InjectClientSizeChangedCallback(gameAssembly);
            InjectUpdateCallback(gameAssembly);
            InjectLoadedGameCallback(gameAssembly);
            InjectChangeDropDownOptionsCallback(gameAssembly);
            InjectSetDropDownToProperValueCallback(gameAssembly);
            InjectGetRandomNameCallback(gameAssembly);
            InjectGetOtherFarmerNamesCallback(gameAssembly);
            InjectParseTextCallback(gameAssembly);
            InjectSpriteTextDrawStringCallback(gameAssembly);
            //InjectSpriteTextGetWidthOfStringCallback(gameAssembly);
            InjectSpriteBatchDrawString(gameAssembly, injectees);
            InjectSpriteFontMeasureString(gameAssembly, injectees);

            gameAssembly.Write(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), pathcedName));

            StartGame(pathcedName);
        }

        static void InjectClientSizeChangedCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("ClientSizeChangedCallback", new Type[] { });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.Game1", "Window_ClientSizeChanged", "(System.Object,System.EventArgs)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();
            processor.InsertBefore(injecteeInstructions[0], processor.Create(OpCodes.Call, Callback));
        }
        static void InjectUpdateCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("UpdateCallback", new Type[] { });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.Game1", "Update", "(Microsoft.Xna.Framework.GameTime)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();
            processor.InsertBefore(injecteeInstructions[injecteeInsCount - 1], processor.Create(OpCodes.Call, Callback));
        }
        static void InjectLoadedGameCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("LoadedGameCallback", new Type[] { });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.Game1", "loadForNewGame", "(System.Boolean)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();
            processor.InsertBefore(injecteeInstructions[injecteeInsCount - 1], processor.Create(OpCodes.Call, Callback));
        }
        static void InjectChangeDropDownOptionsCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("ChangeDropDownOptionCallback", new Type[] { typeof(int), typeof(int), typeof(List<string>) });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);            

            var injectee = gameAssembly.GetMethod("StardewValley.Options", "changeDropDownOption", "(System.Int32,System.Int32,System.Collections.Generic.List`1)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();

            var callInstruction = processor.Create(OpCodes.Call, Callback);

            processor.InsertBefore(injecteeInstructions[0], callInstruction);
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_3));
        }
        static void InjectSetDropDownToProperValueCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SetDropDownToProperValueCallback", new Type[] { typeof(object) });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.Options", "setDropDownToProperValue", "(StardewValley.Menus.OptionsDropDown)System.Void");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var injecteeInsCount = injecteeInstructions.Count;
            var processor = injecteeBody.GetILProcessor();

            var callInstruction = processor.Create(OpCodes.Call, Callback);

            processor.InsertBefore(injecteeInstructions[0], callInstruction);
            processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_1));
        }
        static void InjectGetRandomNameCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("GetRandomNameCallback", new Type[] { });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = gameAssembly.MainModule.Import(hasReturnValue.GetMethod);
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = gameAssembly.MainModule.Import(eventReturnValue.GetMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.Dialogue", "randomName", "()System.String");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectGetOtherFarmerNamesCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("GetOtherFarmerNamesCallback", new Type[] { });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = gameAssembly.MainModule.Import(hasReturnValue.GetMethod);
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = gameAssembly.MainModule.Import(eventReturnValue.GetMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.Utility", "getOtherFarmerNames", "()System.Collections.Generic.List`1");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectParseTextCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("ParseTextCallback", new Type[] { typeof(string), typeof(object), typeof(int) });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = gameAssembly.MainModule.Import(hasReturnValue.GetMethod);
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = gameAssembly.MainModule.Import(eventReturnValue.GetMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.Game1", "parseText", "(System.String,Microsoft.Xna.Framework.Graphics.SpriteFont,System.Int32)System.String");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);

            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectSpriteTextDrawStringCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteTextDrawStringCallback",
                new Type[] {
                    typeof(object),
                    typeof(string),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(float),
                    typeof(float),
                    typeof(bool),
                    typeof(int),
                    typeof(string),
                    typeof(int)
                });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = gameAssembly.MainModule.Import(hasReturnValue.GetMethod);
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = gameAssembly.MainModule.Import(eventReturnValue.GetMethod);

            var injectee = gameAssembly.GetMethod(
                "StardewValley.BellsAndWhistles.SpriteText",
                "drawString",
                "(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.String,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Single,System.Single,System.Boolean,System.Int32,System.String,System.Int32)System.Void"
                );
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];

            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_1));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_2));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_3));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 4));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 5));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 6));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 7));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 8));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 9));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 10));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 11));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg, 12));

            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, injectionPoint));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
        }
        static void InjectSpriteTextGetWidthOfStringCallback(AssemblyDefinition gameAssembly)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteTextGetWidthOfStringCallback", new Type[] { typeof(string) });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);

            var hasReturnValue = typeof(DetourEvent).GetProperty("ReturnEarly");
            var hasReturnValueImport = gameAssembly.MainModule.Import(hasReturnValue.GetMethod);
            var eventReturnValue = typeof(DetourEvent).GetProperty("ReturnValue");
            var eventReturnValueImport = gameAssembly.MainModule.Import(eventReturnValue.GetMethod);

            var injectee = gameAssembly.GetMethod("StardewValley.BellsAndWhistles.SpriteText", "getWidthOfString", "(System.String)System.Int32");
            var injecteeBody = injectee.Body;
            var injecteeInstructions = injecteeBody.Instructions;
            var processor = injecteeBody.GetILProcessor();

            var injectionPoint = injecteeInstructions[0];
            var jmpTarget = processor.Create(OpCodes.Pop);
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, Callback));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Dup));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, hasReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Brfalse, jmpTarget));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Call, eventReturnValueImport));
            processor.InsertBefore(injectionPoint, processor.Create(OpCodes.Ret));
            processor.InsertBefore(injectionPoint, jmpTarget);
        }
        static void InjectSpriteBatchDrawString(AssemblyDefinition gameAssembly, List<MethodDefinition> injectees)
        {
            var FullCallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteBatchDrawStringCallback", new Type[] { typeof(SpriteBatch),
            typeof(SpriteFont),typeof(string),typeof(Vector2),typeof(Color),typeof(float),typeof(Vector2),typeof(float),typeof(SpriteEffects),typeof(float)});
            var FullCallback = gameAssembly.MainModule.Import(FullCallbackMethod);
            var ShortCallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteBatchDrawStringCallback", new Type[] { typeof(SpriteBatch),
            typeof(SpriteFont),typeof(string),typeof(Vector2),typeof(Color) });
            var ShortCallback = gameAssembly.MainModule.Import(ShortCallbackMethod);

            int count = 0;
            foreach (var body in injectees.Select(m => m.Body))
            {
                var processor = body.GetILProcessor();
                var instructions = body.Instructions.Where(instr => instr.OpCode == OpCodes.Callvirt && instr.ToString().Contains("SpriteBatch::DrawString")).ToList();
                foreach (var instr in instructions)
                {
                    var meth = instr.Operand as MethodReference;
                    if (meth.Parameters.Count == 9)
                    {
                        var writeInstruction = processor.Create(OpCodes.Call, FullCallback);
                        processor.Replace(instr, writeInstruction);
                    }
                    else if (meth.Parameters.Count == 4)
                    {
                        var writeInstruction = processor.Create(OpCodes.Call, ShortCallback);
                        processor.Replace(instr, writeInstruction);
                    }
                    count++;
                }
            }
        }
        static void InjectSpriteFontMeasureString(AssemblyDefinition gameAssembly, List<MethodDefinition> injectees)
        {
            var CallbackMethod = typeof(LocalizationBridge).GetMethod("SpriteFontMeasureStringCallback", new Type[] { typeof(SpriteFont), typeof(string) });
            var Callback = gameAssembly.MainModule.Import(CallbackMethod);
            foreach (var body in injectees.Select(m => m.Body))
            {
                var processor = body.GetILProcessor();
                var instructions = body.Instructions.Where(instr => instr.OpCode == OpCodes.Callvirt && instr.ToString().Contains("SpriteFont::MeasureString")).ToList();
                foreach (var instr in instructions)
                {
                    var meth = instr.Operand as MethodReference;
                    var writeInstruction = processor.Create(OpCodes.Call, Callback);
                    processor.Replace(instr, writeInstruction);
                }
            }

        }

        static void StartGame(string name)
        {
            var assembly = Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name));
            assembly.EntryPoint.Invoke(null, new object[] { new string[] { } });
        }
    }   
    
    public static class CecilUtils
    {
        public static string DescriptionOf(MethodDefinition md)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            var set = false;
            foreach (var param in md.Parameters)
            {
                sb.Append(param.ParameterType.Resolve().FullName);
                sb.Append(',');
                set = true;
            }
            if (set) sb.Length -= 1;

            sb.Append(')');
            sb.Append(md.ReturnType.Resolve().FullName);
            return sb.ToString();
        }

        public static bool IsGettingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Ldfld || ins.OpCode == OpCodes.Ldflda;
        }

        public static bool IsPuttingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Stfld;
        }

        public static bool IsNativeType(string returnName)
        {
            return returnName.Equals(typeof(long).FullName) ||
                returnName.Equals(typeof(ulong).FullName) ||
                returnName.Equals(typeof(int).FullName) ||
                returnName.Equals(typeof(uint).FullName) ||
                returnName.Equals(typeof(short).FullName) ||
                returnName.Equals(typeof(ushort).FullName) ||
                returnName.Equals(typeof(byte).FullName) ||
                returnName.Equals(typeof(bool).FullName);
        }

        public static bool IsJump(OpCode oc)
        {
            return
                oc == OpCodes.Br || oc == OpCodes.Br_S ||
                oc == OpCodes.Brtrue || oc == OpCodes.Brtrue_S ||
                oc == OpCodes.Brfalse || oc == OpCodes.Brfalse_S ||
                oc == OpCodes.Bne_Un || oc == OpCodes.Bne_Un_S ||
                oc == OpCodes.Blt_Un || oc == OpCodes.Blt_Un_S ||
                oc == OpCodes.Ble_Un || oc == OpCodes.Ble_Un_S ||
                oc == OpCodes.Bge_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Bgt_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Beq || oc == OpCodes.Beq_S ||
                oc == OpCodes.Ble || oc == OpCodes.Ble_S ||
                oc == OpCodes.Blt || oc == OpCodes.Blt_S
                ;
        }

        public static MethodDefinition GetMethod(this AssemblyDefinition asm, string type, string name, string desc)
        {
            var tds = asm.Modules.Where(m => m.GetType(type) != null).Select(m => m.GetType(type));
            if (tds.Count() == 0)
            {
                return null;
            }
            if (tds.Count() != 1)
            {
                throw new Exception();
            }
            var td = tds.First();
            return td.Methods.FirstOrDefault(m => m.Name.Equals(name) && DescriptionOf(m).Equals(desc.Replace(" ", string.Empty)));
        }
    }
}