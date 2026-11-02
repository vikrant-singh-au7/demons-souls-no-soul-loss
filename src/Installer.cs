using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using System.Web.Script.Serialization;

static class Installer {
    const string ModName = "Keep carried souls on death; empty soul bloodstain";
    const string Title = "PPSA01341", Version = "01.007.000";
    static JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 16777216 };
    static Dictionary<string,object> Read(string p) { return Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(p)); }
    static void Write(string p, object value) {
        string temp = p + ".tmp-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(temp, Json.Serialize(value), new UTF8Encoding(false));
        if (File.Exists(p)) File.Replace(temp, p, null); else File.Move(temp, p);
    }
    static string Value(Dictionary<string,object> d, string k) { return d.ContainsKey(k) ? Convert.ToString(d[k]) : ""; }
    static List<object> Mods(Dictionary<string,object> d) {
        if (!d.ContainsKey("mods") || !(d["mods"] is IEnumerable) || d["mods"] is string) throw new Exception("Invalid existing patch: mods is not an array.");
        var result = new List<object>();
        foreach(object o in (IEnumerable)d["mods"]) {
            if (!(o is Dictionary<string,object>)) throw new Exception("Invalid mod entry in existing patch.");
            result.Add(o);
        }
        return result;
    }
    static Dictionary<string,object> Patch() {
        using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("patch.json"))
        using(var r = new StreamReader(s)) return Json.Deserialize<Dictionary<string,object>>(r.ReadToEnd());
    }
    static string Root(string folder, bool checkProcesses) {
        string root = Path.GetFullPath(folder.Trim());
        if (!File.Exists(Path.Combine(root,"kyty_emulator.exe"))) throw new Exception("Choose the KytyPS5 folder containing kyty_emulator.exe.");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new Exception("Choose a real folder, not a directory link.");
        foreach(string sub in new [] {"_Patches","KeepSoulsInstaller","_SaveData"}) {
            string p=Path.Combine(root,sub);
            if (Directory.Exists(p) && (File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0) throw new Exception("Linked folder is not supported: "+sub);
        }
        if (checkProcesses) foreach(string name in new [] {"kyty_emulator","KytyLauncher","launcher"})
            foreach(var p in Process.GetProcessesByName(name)) { p.Dispose(); throw new Exception("Close KytyPS5 and its launcher before installing or removing this patch."); }
        return root;
    }
    static void CopyTree(string src,string dst) {
        if ((File.GetAttributes(src)&FileAttributes.ReparsePoint)!=0) throw new Exception("Save backup encountered a linked directory.");
        Directory.CreateDirectory(dst);
        foreach(string f in Directory.GetFiles(src)) {
            if ((File.GetAttributes(f)&FileAttributes.ReparsePoint)!=0) throw new Exception("Save backup encountered a linked file.");
            File.Copy(f,Path.Combine(dst,Path.GetFileName(f)));
        }
        foreach(string sub in Directory.GetDirectories(src)) CopyTree(sub,Path.Combine(dst,Path.GetFileName(sub)));
    }
    static string Backup(string root,string label,bool saves) {
        string b=Path.Combine(root,"KeepSoulsInstaller",label+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));
        Directory.CreateDirectory(b);
        string patch=Path.Combine(root,"_Patches",Title+".json"), settings=Path.Combine(root,"launcher-settings.json");
        if(File.Exists(patch)) File.Copy(patch,Path.Combine(b,Title+".json"));
        if(File.Exists(settings)) File.Copy(settings,Path.Combine(b,"launcher-settings.json"));
        if(saves && Directory.Exists(Path.Combine(root,"_SaveData",Title))) CopyTree(Path.Combine(root,"_SaveData",Title),Path.Combine(b,"SaveData-"+Title));
        return b;
    }
    static void ValidateGame(Dictionary<string,object> settings) {
        string game=Value(settings,"game");
        if(String.IsNullOrEmpty(game) || !Directory.Exists(game)) return;
        string param=Path.Combine(game,"sce_sys","param.json");
        if(!File.Exists(param)) return;
        var p=Read(param);
        if(Value(p,"titleId")!=Title || Value(p,"contentVersion")!=Version)
            throw new Exception("The selected game is not PPSA01341 version 01.007.000. No changes made.");
    }
    public static string Install(string folder,bool backupSaves,bool enable,bool checkProcesses) {
        string root=Root(folder,checkProcesses), path=Path.Combine(root,"_Patches",Title+".json");
        string settingsPath=Path.Combine(root,"launcher-settings.json");
        var settings=File.Exists(settingsPath)?Read(settingsPath):new Dictionary<string,object>();
        ValidateGame(settings);
        var fresh=Patch();
        var data=File.Exists(path)?Read(path):fresh;
        if(Value(data,"id")!=Title || Value(data,"version")!=Version || Value(data,"process")!="eboot.bin")
            throw new Exception("An existing patch targets another game/version. It will not be overwritten.");
        var list=Mods(data); int found=-1;
        for(int i=0;i<list.Count;i++) if(Value((Dictionary<string,object>)list[i],"name")==ModName) {
            if(found>=0) throw new Exception("Duplicate keep-souls entries found; resolve them before installing.");
            found=i;
        }
        var statePath=Path.Combine(root,"KeepSoulsInstaller","state.json");
        Dictionary<string,object> state=File.Exists(statePath)?Read(statePath):new Dictionary<string,object>();
        bool first=!state.ContainsKey("installed") || !(bool)state["installed"];
        if(first) {
            state["previousMod"]=File.Exists(path) && found>=0?list[found]:null;
            state["hadCheatsKey"]=settings.ContainsKey("cheats");
            state["previousCheats"]=settings.ContainsKey("cheats")?settings["cheats"]:false;
        }
        var newMod=Mods(fresh)[0];
        if(found>=0) list[found]=newMod; else list.Add(newMod);
        data["mods"]=list;
        string backup=Backup(root,"install",backupSaves);
        Directory.CreateDirectory(Path.Combine(root,"_Patches"));
        byte[] oldPatch=File.Exists(path)?File.ReadAllBytes(path):null;
        byte[] oldSettings=File.Exists(settingsPath)?File.ReadAllBytes(settingsPath):null;
        try {
            Write(path,data);
            if(enable) { settings["cheats"]=true; Write(settingsPath,settings); }
            state["installed"]=true; state["changedCheats"]=enable || (!first && state.ContainsKey("changedCheats") && (bool)state["changedCheats"]);
            state["backup"]=backup; Write(statePath,state);
        } catch {
            if(oldPatch==null) {if(File.Exists(path)) File.Delete(path);} else File.WriteAllBytes(path,oldPatch);
            if(oldSettings==null) {if(File.Exists(settingsPath)) File.Delete(settingsPath);} else File.WriteAllBytes(settingsPath,oldSettings);
            throw;
        }
        return "Installed for PPSA01341 v01.007.000.\r\n\r\n"+(enable?"Cheats enabled. Existing enabled cheats also remain enabled.":"Enable Cheats in the launcher to use the patch.")+"\r\n\r\nRestart KytyPS5. Game files were not modified.\r\nBackup: "+backup;
    }
    public static string Remove(string folder,bool checkProcesses) {
        string root=Root(folder,checkProcesses), path=Path.Combine(root,"_Patches",Title+".json");
        string statePath=Path.Combine(root,"KeepSoulsInstaller","state.json");
        if(!File.Exists(statePath)) throw new Exception("This installer has no installation record in that folder. You can disable Cheats manually instead.");
        var state=Read(statePath);
        if(!state.ContainsKey("installed") || !(bool)state["installed"]) return "The installer patch is already removed.";
        if(!File.Exists(path)) throw new Exception("Patch file is missing; no settings were changed.");
        var data=Read(path); var list=Mods(data); int found=-1;
        for(int i=0;i<list.Count;i++) if(Value((Dictionary<string,object>)list[i],"name")==ModName) found=i;
        if(found<0) throw new Exception("The installed mod entry is missing; no settings were changed.");
        string backup=Backup(root,"remove",false);
        if(state.ContainsKey("previousMod") && state["previousMod"]!=null) list[found]=state["previousMod"]; else list.RemoveAt(found);
        data["mods"]=list; Write(path,data);
        string sp=Path.Combine(root,"launcher-settings.json");
        if(state.ContainsKey("changedCheats") && (bool)state["changedCheats"] && File.Exists(sp)) {
            bool otherEnabled=false;
            foreach(Dictionary<string,object> m in list) if(!m.ContainsKey("enabled") || (bool)m["enabled"]) otherEnabled=true;
            if(!otherEnabled) {
                var s=Read(sp);
                if((bool)state["hadCheatsKey"]) s["cheats"]=state["previousCheats"]; else s.Remove("cheats");
                Write(sp,s);
            }
        }
        state["installed"]=false; Write(statePath,state);
        return "Removed the installer patch; other mods and saves were preserved.\r\nRestart the game.\r\nBackup: "+backup;
    }
    static void Assert(bool yes,string why) {if(!yes) throw new Exception("Self-test failed: "+why);}
    static int SelfTest() {
        string root=Path.Combine(Path.GetTempPath(),"KeepSoulsTest-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root,"kyty_emulator.exe"),"test fixture - never executed");
        Write(Path.Combine(root,"launcher-settings.json"),new Dictionary<string,object>{{"cheats",false},{"extra_args","keep-me"},{"quality","standard"}});
        Directory.CreateDirectory(Path.Combine(root,"_SaveData",Title)); File.WriteAllText(Path.Combine(root,"_SaveData",Title,"save.dat"),"unchanged");
        Install(root,true,true,false);
        Assert((bool)Read(Path.Combine(root,"launcher-settings.json"))["cheats"],"enable cheats");
        Install(root,true,true,false);
        var data=Read(Path.Combine(root,"_Patches",Title+".json")); Assert(Mods(data).Count==1,"idempotent install");
        Remove(root,false);
        var s=Read(Path.Combine(root,"launcher-settings.json")); Assert(!(bool)s["cheats"] && Value(s,"extra_args")=="keep-me","restore settings, preserve unrelated options");
        Assert(Mods(Read(Path.Combine(root,"_Patches",Title+".json"))).Count==0,"remove only installer mod");
        Assert(File.ReadAllText(Path.Combine(root,"_SaveData",Title,"save.dat"))=="unchanged","save unchanged");
        data=Patch(); var mods=Mods(data); mods.Clear(); mods.Add(new Dictionary<string,object>{{"name","Other mod"},{"enabled",true},{"memory",new object[0]}}); data["mods"]=mods; Write(Path.Combine(root,"_Patches",Title+".json"),data);
        Install(root,false,true,false); Remove(root,false);
        Assert(Mods(Read(Path.Combine(root,"_Patches",Title+".json"))).Count==1,"preserve existing mod");
        Assert((bool)Read(Path.Combine(root,"launcher-settings.json"))["cheats"],"leave other enabled mod usable");
        data=Read(Path.Combine(root,"_Patches",Title+".json")); data["version"]="01.005.000"; Write(Path.Combine(root,"_Patches",Title+".json"),data);
        bool refused=false; try {Install(root,false,true,false);} catch {refused=true;} Assert(refused,"reject incompatible existing patch");
        File.WriteAllText(Path.Combine(root,"SELF-TEST-PASSED.txt"),"Install/reinstall/remove/merge/version guard/save preservation passed.");
        return 0;
    }
    [STAThread] static int Main(string[] args) {
        if(args.Length==1 && args[0]=="--self-test") {try{return SelfTest();}catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"KeepSoulsInstaller-test-error.txt"),e.ToString());return 1;}}
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        var form=new Form {Text="Demon's Souls - Keep Souls Installer v0.1",Width=680,Height=440,StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false};
        var info=new Label {Left=20,Top=18,Width=620,Height=85,Text="Keeps carried souls after death; new bloodstains contain zero souls.\r\nFor Demon's Souls PS5 PPSA01341 v01.007.000 with KytyPS5 game-patch support.\r\nExperimental: initial gameplay test passed; all death paths are not verified.\r\nClose the game and launcher first. Original game files are never edited."};
        var box=new TextBox {Left=20,Top=115,Width=500};
        string local=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if(File.Exists(Path.Combine(local,"kyty_emulator.exe"))) box.Text=local;
        var browse=new Button {Left=530,Top=113,Width=100,Text="Browse..."};
        browse.Click+=(s,e)=>{using(var d=new FolderBrowserDialog {Description="Choose the folder containing kyty_emulator.exe",ShowNewFolderButton=false}) if(d.ShowDialog()==DialogResult.OK) box.Text=d.SelectedPath;};
        var saves=new CheckBox {Left=20,Top=155,Width=600,Checked=true,Text="Back up this game's saves before installing (recommended)"};
        var enable=new CheckBox {Left=20,Top=185,Width=620,Checked=true,Text="Enable Cheats in the launcher (also activates existing enabled cheats)"};
        var status=new Label {Left=20,Top=225,Width=620,Height=65,Text="Select your KytyPS5 folder. Other graphics settings and the flicker workaround stay unchanged."};
        var install=new Button {Left=20,Top=310,Width=190,Height=40,Text="Install / Update patch"};
        var remove=new Button {Left=230,Top=310,Width=190,Height=40,Text="Uninstall patch"};
        var close=new Button {Left=440,Top=310,Width=190,Height=40,Text="Close"};
        Action<bool> run=(isInstall)=>{
            if(String.IsNullOrWhiteSpace(box.Text)){MessageBox.Show("Choose a KytyPS5 folder first.");return;}
            install.Enabled=remove.Enabled=false; form.UseWaitCursor=true;
            try {string message=isInstall?Install(box.Text,saves.Checked,enable.Checked,true):Remove(box.Text,true); status.Text=isInstall?"Installation complete.":"Uninstalled.";MessageBox.Show(form,message,"Keep Souls",MessageBoxButtons.OK,MessageBoxIcon.Information);}
            catch(Exception ex){MessageBox.Show(form,ex.Message,"No installation completed",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally {install.Enabled=remove.Enabled=true;form.UseWaitCursor=false;}
        };
        install.Click+=(s,e)=>run(true); remove.Click+=(s,e)=>run(false); close.Click+=(s,e)=>form.Close();
        form.Controls.AddRange(new Control[]{info,box,browse,saves,enable,status,install,remove,close});
        Application.Run(form); return 0;
    }
}
