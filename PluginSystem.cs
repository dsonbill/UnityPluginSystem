//Copyright (c) 2015 Christopher Andrews, Alexandre Oliveira, Joshua Blake, William Donaldson.
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//    SOFTWARE.




using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public class PluginSystem : MonoBehaviour
{
    readonly List<IGamePlugin> loadedPlugins = new List<IGamePlugin>();

    public PluginSystem()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        //This will find and return the assembly requested if it is already loaded
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName == args.Name)
            {
                Debug.Log("Resolved plugin assembly reference: " + args.Name);
                return assembly;
            }
        }

        Debug.Log("Could not resolve assembly " + args.Name);
        return null;
    }

    public void LoadPlugins()
    {
        Debug.Log("Loading plugins...");
        if (!Directory.Exists (Path.Combine(Environment.CurrentDirectory, "Plugins"))) {
            Directory.CreateDirectory (Path.Combine(Environment.CurrentDirectory, "Plugins"));
        }

        //Load the assemblies from files
        List<Assembly> assemblies = new List<Assembly> ();
        string[] fileList = Directory.GetFiles (Path.Combine(Environment.CurrentDirectory, "Plugins"), "*", SearchOption.AllDirectories);
        foreach (string pluginPath in fileList) {
            if (Path.GetExtension (pluginPath).ToLower () == ".dll") {
                try {
                    Assembly currentAssembly = Assembly.LoadFile (pluginPath);
                    assemblies.Add (currentAssembly);
                } catch (Exception e) {
                    //Something went horribly wrong - do an LogExcep for them
                    Debug.Log("Error loading " + pluginPath);
                    Debug.LogException(e);
                }
            }
        }

        Type gameInterfaceType = typeof(IGamePlugin);

        foreach (Assembly loadedAssembly in assemblies)
        {
            Type[] loadedTypes = loadedAssembly.GetExportedTypes();
            foreach (Type loadedType in loadedTypes)
            {
                Type[] typeInterfaces = loadedType.GetInterfaces();
                bool containsPluginInterface = false;
                foreach (Type typeInterface in typeInterfaces)
                {
                    if (typeInterface == gameInterfaceType)
                    {
                        containsPluginInterface = true;
                    }
                }
                if (containsPluginInterface)
                {
                    Debug.Log("Loading plugin: " + loadedType.FullName);

                    try
                    {
                        IGamePlugin pluginInstance = ActivatePluginType(loadedType);

                        if (pluginInstance != null)
                        {
                            Debug.Log("Loaded plugin: " + loadedType.FullName);
                            loadedPlugins.Add(pluginInstance);
                        }
                    }
                    catch (Exception excep)
                    {
                        Debug.Log ("Error loading plugin " + loadedType.FullName +
                            "(" + loadedType.Assembly.FullName + ")");
                        Debug.LogException(excep);
                    }
                }
            }
        }
        Debug.Log("Done!");
    }

    private IGamePlugin ActivatePluginType(Type loadedType)
    {
        try
        {
            //"as IGamePlugin" will cast or return null if the type is not a IGamePlugin
            IGamePlugin pluginInstance = Activator.CreateInstance(loadedType) as IGamePlugin;
            return pluginInstance;
        }
        catch (Exception e)
        {
            Debug.Log ("Cannot activate plugin " + loadedType.Name);
            Debug.LogException(e);
            return null;
        }
    }

    //Fire Update
    public void Update()
    {
        foreach (var plugin in loadedPlugins)
        {
            try
            {
                plugin.Update();
            }
            catch (Exception e)
            {
                Type type = plugin.GetType();
                Debug.Log ("Error thrown in Update event for " +
                    type.FullName + " (" + type.Assembly.FullName + ")");
                Debug.LogException(e);
            }
        }
    }

    //Fire Initialize
    public void Awake()
    {
        DontDestroyOnLoad(this);
        LoadPlugins();
        foreach (var plugin in loadedPlugins)
        {
            try
            {
                plugin.Initialize();
            }
            catch (Exception e)
            {
                Type type = plugin.GetType();
                Debug.Log ( "Exception thrown in Initialize event for " +
                    type.FullName + " (" + type.Assembly.FullName + ")");
                Debug.LogException(e);
            }
        }
    }

    //Fire Draw
    public void OnGUI()
    {
        foreach (var plugin in loadedPlugins)
        {
            try
            {
                plugin.OnGUI();
            }
            catch (Exception e)
            {
                Type type = plugin.GetType();
                Debug.Log ("Exception thrown in Draw event for " +
                    type.FullName + " (" + type.Assembly.FullName + ")");
                Debug.LogException(e);
            }
        }
    }
}