using System;
using System.Reflection;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace TunaLoader
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        private string modsFolderPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "FISH_Data", "Managed", "Mods");
        private string managedFolderPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "FISH_Data", "Managed");
        private const string CurrentVersion = "1.2";

        private const string VersionUrl = "https://raw.githubusercontent.com/coolbeansguy/TUNALOADER/main/version.txt";
        private const string DownloadUrl = "https://raw.githubusercontent.com/coolbeansguy/TUNALOADER/main/TunaLoader.zip";

        private System.Windows.Forms.Panel dropZonePanel = null!;
        private System.Windows.Forms.Label dropZoneLabel = null!;
        private System.Windows.Forms.Button installButton = null!;
        private System.Windows.Forms.Button uninstallButton = null!;
        private System.Windows.Forms.ListBox modsList = null!;
        private System.Windows.Forms.Button enableModButton = null!;
        private System.Windows.Forms.Button disableModButton = null!;
        private System.Windows.Forms.Button removeModButton = null!;
        private System.Windows.Forms.Label statusLabel = null!;

        private System.Collections.Generic.Dictionary<string, bool> modStates = new System.Collections.Generic.Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        private string modMetadataFile => System.IO.Path.Combine(modsFolderPath, "mods_metadata.json");

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "Form1";
            this.Text = "Tuna Loader Panel";
            this.ResumeLayout(false);
        }

        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);

            if (!ValidateOfficialGameInstallation())
            {
                System.Windows.Forms.MessageBox.Show("Tuna Loader Security Error:\n\nOfficial game files could not be verified in this folder.\nPlease place TunaLoader.exe inside the main installation folder alongside FISH.exe!", "Validation Failed", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
                this.Close();
                return;
            }

            System.IO.Directory.CreateDirectory(modsFolderPath);

            this.Text = $"Tuna Loader v{CurrentVersion} Manager & Installer";
            this.Size = new System.Drawing.Size(520, 660);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(20, 20, 24);
            this.Padding = new System.Windows.Forms.Padding(22);

            BuildUserInterface();
            UpdateInstallationStatus();

            _ = CheckForUpdatesAsync();
        }

        private void BuildUserInterface()
        {
            Controls.Clear();

            System.Windows.Forms.Panel bottomContainer = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Bottom, Height = 175 };

            statusLabel = new System.Windows.Forms.Label
            {
                Text = "TUNA LOADER STATUS: CHECKING...",
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                Dock = System.Windows.Forms.DockStyle.Top,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Height = 30
            };
            bottomContainer.Controls.Add(statusLabel);

            installButton = new System.Windows.Forms.Button
            {
                Text = "Install TunaLoader to Game",
                Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                BackColor = System.Drawing.Color.FromArgb(0, 165, 80),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 50,
                Cursor = System.Windows.Forms.Cursors.Hand
            };
            installButton.FlatAppearance.BorderSize = 0;
            installButton.Click += (s, e) => RunPermanentInstallation();
            bottomContainer.Controls.Add(installButton);

            bottomContainer.Controls.Add(new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Top, Height = 10 });

            uninstallButton = new System.Windows.Forms.Button
            {
                Text = "Uninstall Loader (Restore Vanilla Game Files)",
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                BackColor = System.Drawing.Color.FromArgb(200, 60, 60),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 45,
                Cursor = System.Windows.Forms.Cursors.Hand
            };
            uninstallButton.FlatAppearance.BorderSize = 0;
            uninstallButton.Click += (s, e) => RunPermanentUninstallation();
            bottomContainer.Controls.Add(uninstallButton);

            Controls.Add(bottomContainer);

            Controls.Add(new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Bottom, Height = 15 });

            dropZonePanel = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Fill, BackColor = System.Drawing.Color.FromArgb(30, 30, 36), Padding = new System.Windows.Forms.Padding(12) };
            dropZonePanel.AllowDrop = true;
            dropZonePanel.DragEnter += MainForm_DragEnter;
            dropZonePanel.DragLeave += MainForm_DragLeave;
            dropZonePanel.DragDrop += MainForm_DragDrop;
            Controls.Add(dropZonePanel);

            modsList = new System.Windows.Forms.ListBox
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 160,
                BackColor = System.Drawing.Color.FromArgb(22, 22, 26),
                ForeColor = System.Drawing.Color.FromArgb(210, 210, 220),
                Font = new System.Drawing.Font("Consolas", 10, System.Drawing.FontStyle.Regular),
                BorderStyle = System.Windows.Forms.BorderStyle.None
            };
            modsList.AllowDrop = true;
            modsList.DragEnter += MainForm_DragEnter;
            modsList.DragLeave += MainForm_DragLeave;
            modsList.DragDrop += MainForm_DragDrop;
            dropZonePanel.Controls.Add(modsList);

            System.Windows.Forms.FlowLayoutPanel modsButtons = new System.Windows.Forms.FlowLayoutPanel { Dock = System.Windows.Forms.DockStyle.Top, Height = 45, FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight, Padding = new System.Windows.Forms.Padding(0, 10, 0, 0), WrapContents = false };

            Action<System.Windows.Forms.Button, System.Drawing.Color> StyleModControl = (btn, borderColor) =>
            {
                btn.Height = 32; btn.Width = 100; btn.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
                btn.FlatStyle = System.Windows.Forms.FlatStyle.Flat; btn.BackColor = System.Drawing.Color.FromArgb(42, 42, 48); btn.ForeColor = System.Drawing.Color.FromArgb(230, 230, 240);
                btn.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold); btn.FlatAppearance.BorderColor = borderColor; btn.FlatAppearance.BorderSize = 1; btn.Cursor = System.Windows.Forms.Cursors.Hand;
            };

            enableModButton = new System.Windows.Forms.Button { Text = "Enable" }; StyleModControl(enableModButton, System.Drawing.Color.FromArgb(0, 165, 80)); enableModButton.Click += (s, e) => EnableSelectedMod(); modsButtons.Controls.Add(enableModButton);
            disableModButton = new System.Windows.Forms.Button { Text = "Disable" }; StyleModControl(disableModButton, System.Drawing.Color.FromArgb(215, 90, 40)); disableModButton.Click += (s, e) => DisableSelectedMod(); modsButtons.Controls.Add(disableModButton);
            removeModButton = new System.Windows.Forms.Button { Text = "Remove" }; StyleModControl(removeModButton, System.Drawing.Color.FromArgb(200, 50, 50)); removeModButton.Click += (s, e) => RemoveSelectedMod(); modsButtons.Controls.Add(removeModButton);

            dropZonePanel.Controls.Add(modsButtons);

            dropZoneLabel = new System.Windows.Forms.Label { Text = "📥\nDRAG & DROP NEW MODS HERE\n(Accepts compiled .dll files)", Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold), ForeColor = System.Drawing.Color.FromArgb(140, 140, 160), TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Dock = System.Windows.Forms.DockStyle.Fill };
            dropZoneLabel.AllowDrop = true;
            dropZoneLabel.DragEnter += MainForm_DragEnter;
            dropZoneLabel.DragLeave += MainForm_DragLeave;
            dropZoneLabel.DragDrop += MainForm_DragDrop;
            dropZonePanel.Controls.Add(dropZoneLabel);

            LoadModMetadata();
            RefreshInstalledMods();
        }

        private void UpdateInstallationStatus()
        {
            string backupCleanPath = System.IO.Path.Combine(managedFolderPath, "Assembly-CSharp.dll.vanilla");
            if (System.IO.File.Exists(backupCleanPath))
            {
                statusLabel.Text = "TUNA LOADER STATUS: PERMANENTLY INSTALLED ✔";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(100, 255, 140);
            }
            else
            {
                statusLabel.Text = "TUNA LOADER STATUS: NOT YET INSTALLED";
                statusLabel.ForeColor = System.Drawing.Color.FromArgb(240, 100, 100);
            }
        }

        private void RunPermanentInstallation()
        {
            string targetDllPath = System.IO.Path.Combine(managedFolderPath, "Assembly-CSharp.dll");
            string backupCleanPath = System.IO.Path.Combine(managedFolderPath, "Assembly-CSharp.dll.vanilla");

            try
            {
                if (!System.IO.File.Exists(backupCleanPath))
                {
                    System.IO.File.Copy(targetDllPath, backupCleanPath, false);
                }

                ExecuteAssemblyBake(backupCleanPath, targetDllPath);
                UpdateInstallationStatus();

                System.Windows.Forms.MessageBox.Show("TunaLoader has been successfully baked into the game engine binary!\n\nYou can now close this window and launch your game directly or via Steam. Your mods directory will run automatically.", "Installation Success", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Bake Routine Failed: {ex.Message}", "Critical Failure", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void RunPermanentUninstallation()
        {
            string targetDllPath = System.IO.Path.Combine(managedFolderPath, "Assembly-CSharp.dll");
            string backupCleanPath = System.IO.Path.Combine(managedFolderPath, "Assembly-CSharp.dll.vanilla");
            string coreDll = System.IO.Path.Combine(managedFolderPath, "TunaCore.dll");

            try
            {
                if (System.IO.File.Exists(backupCleanPath))
                {
                    System.IO.File.Copy(backupCleanPath, targetDllPath, true);
                    System.IO.File.Delete(backupCleanPath);
                }

                if (System.IO.File.Exists(coreDll)) System.IO.File.Delete(coreDll);

                UpdateInstallationStatus();
                System.Windows.Forms.MessageBox.Show("TunaLoader hooks successfully uninstalled. Game files restored to completely vanilla properties.", "Restoration Success", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Restoration Failure: {ex.Message}", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ExecuteAssemblyBake(string sourceCleanDll, string outputPatchedDll)
        {
            var resolver = new Mono.Cecil.DefaultAssemblyResolver();
            resolver.AddSearchDirectory(managedFolderPath);

            string pluginsBase = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "FISH_Data", "Plugins");
            if (System.IO.Directory.Exists(pluginsBase)) resolver.AddSearchDirectory(pluginsBase);

            string plugins64 = System.IO.Path.Combine(pluginsBase, "x86_64");
            if (System.IO.Directory.Exists(plugins64)) resolver.AddSearchDirectory(plugins64);

            var readerParams = new Mono.Cecil.ReaderParameters { AssemblyResolver = resolver };

            string localCorePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TunaCore.dll");
            string targetCorePath = System.IO.Path.Combine(managedFolderPath, "TunaCore.dll");

            if (System.IO.File.Exists(localCorePath))
            {
                System.IO.File.Copy(localCorePath, targetCorePath, true);
            }
            else
            {
                throw new System.IO.FileNotFoundException("TunaCore.dll dependency could not be located in build runtime folder.");
            }

            using (var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(sourceCleanDll, readerParams))
            {
                var mainModule = assembly.MainModule;

                var gameControllerType = mainModule.GetTypes().FirstOrDefault(t => System.String.Equals(t.Name, "GameController", System.StringComparison.OrdinalIgnoreCase) || t.FullName.EndsWith(".GameController"));
                if (gameControllerType == null) throw new System.Exception("GameController class structure not found in target environment.");

                var targetMethod = gameControllerType.Methods.FirstOrDefault(m => m.Name == "Awake") ?? gameControllerType.Methods.FirstOrDefault(m => m.Name == "Start");
                if (targetMethod == null) throw new System.Exception("Awake execution sequence missing from game properties.");

                var il = targetMethod.Body.GetILProcessor();
                var firstInstruction = targetMethod.Body.Instructions[0];

                System.Reflection.Assembly coreAsm = System.Reflection.Assembly.LoadFrom(targetCorePath);
                System.Type bootType = coreAsm.GetType("TunaCore.Bootstrapper")!;
                System.Reflection.MethodInfo initMethod = bootType.GetMethod("Initialize")!;

                var importedMethod = mainModule.ImportReference(initMethod);

                il.InsertBefore(firstInstruction, il.Create(Mono.Cecil.Cil.OpCodes.Call, importedMethod));

                assembly.Write(outputPatchedDll);
            }
        }

        private bool ValidateOfficialGameInstallation()
        {
            string exePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "FISH.exe");
            string dataFolderPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "FISH_Data");
            return System.IO.File.Exists(exePath) && System.IO.Directory.Exists(dataFolderPath);
        }

        private void MainForm_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
            {
                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
                dropZonePanel.BackColor = System.Drawing.Color.FromArgb(36, 48, 40);
                dropZoneLabel.ForeColor = System.Drawing.Color.FromArgb(100, 255, 120);
            }
        }

        private void MainForm_DragLeave(object sender, System.EventArgs e)
        {
            dropZonePanel.BackColor = System.Drawing.Color.FromArgb(30, 30, 36);
            dropZoneLabel.ForeColor = System.Drawing.Color.FromArgb(140, 140, 160);
        }

        private void MainForm_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            dropZonePanel.BackColor = System.Drawing.Color.FromArgb(30, 30, 36);
            dropZoneLabel.ForeColor = System.Drawing.Color.FromArgb(140, 140, 160);

            string[] droppedFiles = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);

            foreach (string filePath in droppedFiles)
            {
                string cleanExt = System.IO.Path.GetExtension(filePath).ToLower();
                if (cleanExt == ".dll" || cleanExt == ".disabled")
                {
                    try
                    {
                        string pureName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        if (pureName.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
                        {
                            pureName = pureName.Substring(0, pureName.Length - 4);
                        }

                        string targetFileName = pureName + ".dll";
                        string destinationPath = System.IO.Path.Combine(modsFolderPath, targetFileName);

                        System.IO.File.Copy(filePath, destinationPath, true);
                        System.Windows.Forms.MessageBox.Show($"Successfully added: {targetFileName}!", "Tuna Loader Success", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

                        modStates[targetFileName] = true;
                        SaveModMetadata();
                        RefreshInstalledMods();
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show($"Transfer Error: {ex.Message}", "Tuna Loader Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Tuna Loader only accepts mod library files!", "Invalid File Type", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
        }

        private void RefreshInstalledMods()
        {
            try
            {
                modsList.Items.Clear();
                var allFiles = System.IO.Directory.GetFiles(modsFolderPath, "*.*")
                    .Where(s => s.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".disabled", System.StringComparison.OrdinalIgnoreCase));

                System.Collections.Generic.HashSet<string> processedMods = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

                foreach (var file in allFiles)
                {
                    string filename = System.IO.Path.GetFileName(file);
                    string baseModName = filename;

                    if (filename.EndsWith(".disabled", System.StringComparison.OrdinalIgnoreCase))
                    {
                        baseModName = filename.Substring(0, filename.Length - 9);
                    }

                    if (processedMods.Contains(baseModName)) continue;
                    processedMods.Add(baseModName);

                    bool enabled = !modStates.ContainsKey(baseModName) || modStates[baseModName];
                    string expectedCurrentPath = System.IO.Path.Combine(modsFolderPath, enabled ? baseModName : baseModName + ".disabled");
                    string unexpectedOldPath = System.IO.Path.Combine(modsFolderPath, enabled ? baseModName + ".disabled" : baseModName);

                    try
                    {
                        if (System.IO.File.Exists(unexpectedOldPath))
                        {
                            if (System.IO.File.Exists(expectedCurrentPath)) System.IO.File.Delete(unexpectedOldPath);
                            else System.IO.File.Move(unexpectedOldPath, expectedCurrentPath);
                        }
                    }
                    catch { }

                    var display = enabled ? $" ACTIVE   →  {baseModName}" : $" DISABLED →  {baseModName}";
                    modsList.Items.Add(display);
                }
            }
            catch { }
        }

        private void LoadModMetadata()
        {
            try
            {
                if (!System.IO.File.Exists(modMetadataFile)) return;
                var json = System.IO.File.ReadAllText(modMetadataFile);
                var dict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, bool>>(json);
                if (dict != null) modStates = new System.Collections.Generic.Dictionary<string, bool>(dict, System.StringComparer.OrdinalIgnoreCase);
            }
            catch { }
        }

        private void SaveModMetadata()
        {
            try
            {
                System.IO.File.WriteAllText(modMetadataFile, System.Text.Json.JsonSerializer.Serialize(modStates));
            }
            catch { }
        }

        private string? SelectedModFileName()
        {
            if (modsList.SelectedItem == null) return null;
            var text = modsList.SelectedItem.ToString()!;
            var idx = text.IndexOf("→ ");
            return idx > 0 ? text.Substring(idx + 2).Trim() : text;
        }

        private void EnableSelectedMod()
        {
            var name = SelectedModFileName();
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                modStates[name] = true;
                string disabledPath = System.IO.Path.Combine(modsFolderPath, name + ".disabled");
                string enabledPath = System.IO.Path.Combine(modsFolderPath, name);

                if (System.IO.File.Exists(disabledPath) && !System.IO.File.Exists(enabledPath))
                {
                    System.IO.File.Move(disabledPath, enabledPath);
                }

                SaveModMetadata();
                RefreshInstalledMods();
            }
            catch (System.Exception ex) { System.Windows.Forms.MessageBox.Show($"Error: {ex.Message}"); }
        }

        private void DisableSelectedMod()
        {
            var name = SelectedModFileName();
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                modStates[name] = false;
                string enabledPath = System.IO.Path.Combine(modsFolderPath, name);
                string disabledPath = System.IO.Path.Combine(modsFolderPath, name + ".disabled");

                if (System.IO.File.Exists(enabledPath) && !System.IO.File.Exists(disabledPath))
                {
                    System.IO.File.Move(enabledPath, disabledPath);
                }

                SaveModMetadata();
                RefreshInstalledMods();
            }
            catch (System.Exception ex) { System.Windows.Forms.MessageBox.Show($"Error: {ex.Message}"); }
        }

        private void RemoveSelectedMod()
        {
            var name = SelectedModFileName();
            if (string.IsNullOrEmpty(name)) return;
            if (System.Windows.Forms.MessageBox.Show($"Remove {name} completely from directories?", "Confirm Remove", System.Windows.Forms.MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes) return;
            try
            {
                string enabledPath = System.IO.Path.Combine(modsFolderPath, name);
                string disabledPath = System.IO.Path.Combine(modsFolderPath, name + ".disabled");

                if (System.IO.File.Exists(enabledPath)) System.IO.File.Delete(enabledPath);
                if (System.IO.File.Exists(disabledPath)) System.IO.File.Delete(disabledPath);

                if (modStates.ContainsKey(name)) modStates.Remove(name);

                SaveModMetadata();
                RefreshInstalledMods();
            }
            catch (System.Exception ex) { System.Windows.Forms.MessageBox.Show($"Error: {ex.Message}"); }
        }

        private async System.Threading.Tasks.Task CheckForUpdatesAsync()
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("TunaLoader-Updater");
                    string latestVersionString = (await client.GetStringAsync(VersionUrl)).Trim();

                    if (System.Version.TryParse(latestVersionString, out System.Version? latestVersion) &&
                        System.Version.TryParse(CurrentVersion, out System.Version? currentVersion))
                    {
                        if (latestVersion > currentVersion)
                        {
                            var result = System.Windows.Forms.MessageBox.Show(
                                $"A new update (v{latestVersionString}) is available!\nWould you like to download and install it now?",
                                "Update Available",
                                System.Windows.Forms.MessageBoxButtons.YesNo,
                                System.Windows.Forms.MessageBoxIcon.Information);

                            if (result == System.Windows.Forms.DialogResult.Yes)
                            {
                                await RunAutoUpdateAsync(client);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private async System.Threading.Tasks.Task RunAutoUpdateAsync(System.Net.Http.HttpClient client)
        {
            try
            {
                string currentExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
                string currentExeDir = System.IO.Path.GetDirectoryName(currentExePath)!;
                string currentExeName = System.IO.Path.GetFileName(currentExePath);
                string zipPath = System.IO.Path.Combine(currentExeDir, "update.zip");
                string extractPath = System.IO.Path.Combine(currentExeDir, "UpdateTemp");

                statusLabel.Text = "DOWNLOADING ZIP UPDATE...";
                statusLabel.ForeColor = System.Drawing.Color.Gold;

                byte[] fileBytes = await client.GetByteArrayAsync(DownloadUrl);
                await System.IO.File.WriteAllBytesAsync(zipPath, fileBytes);

                statusLabel.Text = "EXTRACTING FILES...";

                if (System.IO.Directory.Exists(extractPath)) System.IO.Directory.Delete(extractPath, true);
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                System.IO.File.Delete(zipPath);

                statusLabel.Text = "APPLYING UPDATE...";

                string cmdArgs = $"/c timeout /t 2 /nobreak && xcopy \"{extractPath}\" \"{currentExeDir}\" /Y /E && rmdir /S /Q \"{extractPath}\" && start \"\" \"{currentExeName}\"";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArgs,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = currentExeDir
                };

                System.Diagnostics.Process.Start(psi);
                System.Windows.Forms.Application.Exit();
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Zip update failed: {ex.Message}", "Update Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                UpdateInstallationStatus();
            }
        }
    }
}

namespace Mono.Cecil
{
    public class MonoTypeReference : TypeReference
    {
        public MonoTypeReference(ModuleDefinition module, System.Type type)
            : base(type.Namespace, type.Name, module, module.TypeSystem.CoreLibrary) { }
    }
}