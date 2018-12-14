﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using Expression = EnvDTE.Expression;

namespace ObjectDumper
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DumpObjectCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;
        public const int ContextMenuCommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e352a0e9-68b9-427d-87f5-7db47da08b18");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DumpObjectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DumpObjectCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            var menuCommandIDc = new CommandID(CommandSet, ContextMenuCommandId);
            var menuItemc = new MenuCommand(this.ContextMenuExecute, menuCommandIDc);

            commandService.AddCommand(menuItemc);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DumpObjectCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in DumpObjectCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new DumpObjectCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE dte;

            dte = (DTE)this.ServiceProvider.GetServiceAsync(typeof(DTE)).Result;

            var debugger = dte.Debugger;
            var locals = debugger.CurrentStackFrame.Locals;

            var localList = new List<Expression>();
            foreach (Expression item in locals)
            {
                localList.Add(item);
            }
            var dialog = new ExportDialog(localList);
            dialog.ShowDialog();
        }

        private void ContextMenuExecute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE dte;

            dte = (DTE)this.ServiceProvider.GetServiceAsync(typeof(DTE)).Result;
            Document doc = dte.ActiveDocument;
           

            TextDocument txt = doc.Object() as TextDocument;

            var selection = txt.Selection;//dte.ActiveWindow.Selection as TextSelection;// 

            if (selection.IsEmpty)
            {
                //todo
                return;
            }
            var text = selection.Text;
            var debugger = dte.Debugger;
            var locals = debugger.CurrentStackFrame.Locals;

            var localList = new List<Expression>();
            foreach (Expression item in locals)
            {
                if (item.Name == text)
                {
                    var jsongenerator = new JsonGenerator();
                    var json = jsongenerator.GenerateJson(item);
                    Clipboard.SetText(json);
                    return;
                }
            }

            System.Windows.Forms.MessageBox.Show("Could not found a local matching current selection.");


            var anchorPoint = selection.AnchorPoint as TextPoint;

            var editpoint = anchorPoint.CreateEditPoint();
            editpoint.CharRight(1);


            
            var iagret = selection.IsActiveEndGreater;

            CodeElement foundElem = null;
            vsCMElement correct;
            foreach (vsCMElement el in Enum.GetValues( typeof(vsCMElement)))
            {
                var codeIte = editpoint.CodeElement[el];
                if(codeIte != null)
                {
                    foundElem = codeIte;
                    correct = el;
                }
            }

            var codeItem = anchorPoint.CodeElement[vsCMElement.vsCMElementOther];
            var fullname = codeItem.FullName;
                


           
        }
    }
}