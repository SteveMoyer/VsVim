﻿#light

namespace Vim.Modes.Insert
open Vim
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Operations
open Microsoft.VisualStudio.Text.Editor

type CommandFunction = unit -> ProcessResult

type internal InsertMode
    ( 
        _data : IVimBuffer, 
        _operations : Modes.ICommonOperations,
        _broker : IDisplayWindowBroker) as this =

    let mutable _commandMap : Map<KeyInput,CommandFunction> = Map.empty

    do
        let commands : (string * CommandFunction) list = 
            [
                ("<Esc>", this.ProcessEscape);
                ("CTRL-[", this.ProcessEscape);
                ("CTRL-d", this.ProcessShiftLeft)
                ("CTRL-o", this.ProcessNormalModeOneCommand)
            ]

        _commandMap <-
            commands 
            |> Seq.ofList
            |> Seq.map (fun (str,func) -> (KeyNotationUtil.StringToKeyInput str),func)
            |> Map.ofSeq


    /// Enter normal mode for a single command
    member private this.ProcessNormalModeOneCommand() =
        ProcessResult.SwitchModeWithArgument (ModeKind.Normal, ModeArgument.OneTimeCommand ModeKind.Insert)

    /// Process the CTRL-D combination and do a shift left
    member private this.ProcessShiftLeft() = 
        _operations.ShiftLinesLeft 1
        ProcessResult.Processed

    member private this.ProcessEscape () =

        if _broker.IsCompletionActive || _broker.IsSignatureHelpActive || _broker.IsQuickInfoActive then
            _broker.DismissDisplayWindows()

            if _data.Settings.GlobalSettings.DoubleEscape then ProcessResult.Processed
            else 
                _operations.MoveCaretLeft 1 
                ProcessResult.SwitchMode ModeKind.Normal

        else
            _operations.MoveCaretLeft 1 
            ProcessResult.SwitchMode ModeKind.Normal

    interface IMode with 
        member x.VimBuffer = _data
        member x.CommandNames =  _commandMap |> Seq.map (fun p -> p.Key) |> Seq.map OneKeyInput
        member x.ModeKind = ModeKind.Insert
        member x.CanProcess ki = Map.containsKey ki _commandMap 
        member x.Process (ki : KeyInput) = 
            match Map.tryFind ki _commandMap with
            | Some(func) -> func()
            | None -> Processed
        member x.OnEnter _ = ()
        member x.OnLeave () = ()
        member x.OnClose() = ()
