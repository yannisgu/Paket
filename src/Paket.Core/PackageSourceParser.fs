﻿module Paket.PackageSourceParser

open Paket
open System
open System.Text.RegularExpressions
open Paket.PackageSources
open Paket.CredentialStore

let userNameRegex = new Regex("username[:][ ]*[\"]([^\"]*)[\"]", RegexOptions.IgnoreCase)
let passwordRegex = new Regex("password[:][ ]*[\"]([^\"]*)[\"]", RegexOptions.IgnoreCase)
let environemntVariableRegex = new Regex("^%\w*%$")

let parseAuth (text : string) (source : string) = 
    if userNameRegex.IsMatch(text) && passwordRegex.IsMatch(text) then 
        let username = AuthEntry.Create <| userNameRegex.Match(text).Groups.[1].Value
        let password = AuthEntry.Create <| passwordRegex.Match(text).Groups.[1].Value
        if (environemntVariableRegex.IsMatch(username.Expanded) && 
            environemntVariableRegex.IsMatch(password.Expanded)) || 
            ((String.IsNullOrEmpty username.Expanded) && 
             (String.IsNullOrEmpty password.Expanded)) 
        then 
            getFromCredentialStore source
        else 
            Some { Username = username
                   Password = password }
    else 
        if text.Contains("username:") || text.Contains("password:") then 
            failwithf "Could not parse auth in \"%s\"" text
        None

let getSources lines = 
    [ for line : string in lines do
          match line.Trim() with
          | trimmed when trimmed.StartsWith "source" -> 
              let parts = trimmed.Split ' '
              let newSource = parts.[1].Replace("\"", "").TrimEnd([| '/' |])
              yield PackageSource.Parse(newSource, (parseAuth trimmed newSource))
          | _ -> () ]
