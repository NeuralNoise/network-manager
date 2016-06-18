﻿
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using NetworkManager.WMIExecution;
using System.Net;

namespace NetworkManager.Domain {

    /// <summary>
    /// A computer in the domain
    /// </summary>
    public class Computer {
        public string name { get; set; }
        public string domain { get; set; }
        public string nameLong { get { return name + "." + domain; } }
        public string os { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public DateTime lastLogOn { get; set; }
        public DateTime creation { get; set; }
        public DateTime lastChange { get; set; }
        public bool isAlive { get; set; }
        public bool isServer {
            get {
                return os.ToLower().Contains("server");
            }
        }

        private static double maxCacheLife = 120000; // 2mn
        private IEnumerable<Software> cachedSoftwares;
        private DateTime lastSoftwaresFetch;
        private IEnumerable<User> cachedUsers;
        private DateTime lastUsersFetch;
        private string cachedMacAddr;
        private DateTime lastMacAddrFetch;


        /// <summary>
        /// Return the first ipv4 of the computer, or null if not found
        /// </summary>
        /// <returns></returns>
        public IPAddress getIpAddress() {
            try {
                foreach (IPAddress ip in Dns.GetHostAddresses(nameLong).ToList()) {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip;
                }
            } catch(Exception e) {} // Ignore errors

            return null; // No ipv4 address
        }

        /// <summary>
        /// Return the mac address of the computer
        /// </summary>
        /// <param name="force">True to ignore any cache</param>
        /// <returns>The mac address</returns>
        public async Task<string> getMacAddress(bool force = false) {
            TimeSpan diff = DateTime.Now - lastMacAddrFetch;

            if (force || diff.TotalMilliseconds > maxCacheLife || cachedMacAddr == null) {
                cachedMacAddr = await WMIExecutor.getMACAddress(this);
                lastMacAddrFetch = DateTime.Now;
            }

            return cachedMacAddr;
        }
        
        /// <summary>
        /// Upload a file to the domain computer. The path should either be a full name including partion
        /// (i.e. C:\test\file.txt) or include a shared folder (i.e. \shared\file.txt)
        /// </summary>
        /// <param name="local">The local filename</param>
        /// <param name="remote">The absolution remote filename</param>
        public void uploadFile(string local, string remote) {
            File.Copy(local, $@"\\{nameLong}\{remote.Replace(':', '$')}", true);
        }

        /// <summary>
        /// Download a file to the local computer from the remote computer. The path should either be a 
        /// full name including partion (i.e. C:\test\file.txt) or include a shared folder (i.e. \shared\file.txt)
        /// </summary>
        /// <param name="remote">The absolution remote filename</param>
        /// <param name="local">The local filename</param>
        public void downloadFile(string remote, string local) {
            File.Copy($@"\\{nameLong}\{remote.Replace(':', '$')}", local);
        }

        /// <summary>
        /// Delete a file from the remote computer. The path should either be a full name including partion
        /// (i.e. C:\test\file.txt) or include a shared folder (i.e. \shared\file.txt)
        /// </summary>
        /// <param name="file">The absolution remote filename</param>
        public void deleteFile(string file) {
            File.Delete($@"\\{nameLong}\{file.Replace(':', '$')}");
        }

        /// <summary>
        /// Execute a remote action
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="args">The command arguments</param>
        /// <param name="maxDuration">The maximum duration of the command</param>
        /// <returns>The async task of the action</returns>
        public async Task<WMIExecutionResult> exec(string command, string[] args = null, long maxDuration = 5000) {
            return await WMIExecutor.exec(this, command, args, maxDuration);
        }

        /// <summary>
        /// Execute a remote action
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="args">The command arguments</param>
        /// <param name="maxDuration">The maximum duration of the command</param>
        /// <param name="callback">The callback to call after the remote action</param>
        public void exec(string command, string[] args, Action<WMIExecutionResult> callback, Action<WMIException> callbackError = null, long maxDuration = 5000) {
            WMIExecutor.exec(this, command, args, maxDuration).ContinueWith(result => {
                if (result.Exception != null)
                    if(callbackError != null)
                        callbackError(result.Exception.InnerException as WMIException);
                else
                    callback(result.Result);
            });
        }

        /// <summary>
        /// Shutdown the computer
        /// </summary>
        public async Task shutdown() {
            await WMIExecutor.shutdown(this);
        }

        /// <summary>
        /// Reboot the computer
        /// </summary>
        public async Task reboot() {
            await WMIExecutor.reboot(this);
        }

        /// <summary>
        /// Start a remote desktop connection to the computer
        /// </summary>
        public void startRemoteDesktop() {
            Process rdcProcess = new Process();
            rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe");
            rdcProcess.StartInfo.Arguments = $"/v {nameLong} /admin";
            rdcProcess.Start();
        }

        /// <summary>
        /// Get a path transposed for the computer
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The network path</returns>
        public string getPath(string path) {
            return $@"\\{nameLong}\{path.Replace(':', '$')}";
        }

        /// <summary>
        /// Return the logged users on the computed. Note that only real user accounts will
        /// be returned
        /// </summary>
        /// <param name="force">True to ignore any cache</param>
        /// <returns>The logged user</returns>
        public async Task<IEnumerable<User>> getLoggedUsers(bool force = false) {
            return (await getAllLoggedUsers(force)).Where(u => u.SIDType == 1);
        }

        /// <summary>
        /// Return all the logged users on the computer. Note that local system account will
        /// also be returned
        /// </summary>
        /// <param name="force">True to ignore any cache</param>
        /// <returns>ALl the logged users</returns>
        public async Task<IEnumerable<User>> getAllLoggedUsers(bool force = false) {
            TimeSpan diff = DateTime.Now - lastUsersFetch;

            if (force || diff.TotalMilliseconds > maxCacheLife || cachedUsers == null) {
                cachedUsers = await WMIExecutor.getLoggedUsers(this);
                lastUsersFetch = DateTime.Now;
            }

            return cachedUsers;
        }

        /// <summary>
        /// Return all the sofwares installed on the computer.
        /// </summary>
        /// <param name="force">True to ignore any cache</param>
        /// <returns>All the installed sofwares</returns>
        public async Task<IEnumerable<Software>> getInstalledSofwares(bool force = false) {
            TimeSpan diff = DateTime.Now - lastSoftwaresFetch;

            if (force || diff.TotalMilliseconds > maxCacheLife || cachedSoftwares == null) {
                var results = await Task.WhenAll(
                    Task.Run(() => WMIExecutor.getInstalledSoftwares(this, 32)),
                    Task.Run(() => WMIExecutor.getInstalledSoftwares(this, 64)));
                cachedSoftwares = results.SelectMany(result => result);
                lastSoftwaresFetch = DateTime.Now;
            }

            return cachedSoftwares;
        }

        /// <summary>
        /// Return true if the computer is the local computer, false otherwise
        /// </summary>
        /// <returns>True if the computer is the local computer, false otherwise</returns>
        public bool isLocalComputer() {
            return Environment.MachineName == name;
        }

        public void copyCache(Computer c) {
            cachedSoftwares = c.cachedSoftwares;
            cachedUsers = c.cachedUsers;
            lastSoftwaresFetch = c.lastSoftwaresFetch;
            lastUsersFetch = c.lastUsersFetch;
        }
    }
}
