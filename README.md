# SharpMik
SharpMik, pure C# mod player
Project Description

SharpMik is a port of the C based MikMod lib to 100% C#.

This is based on MikMod v3.1.12

Currently following the same usage and code style as the C lib, so it can only play one mod at a time and all actions are done via static methods. There is a wrapper class to make using it a little easier.

 

If you are using SharpMik for a project why not let me know in the discussions, if will help me work out the demand for updates and which platforms its being used on!

 

Goal

To reach 100% compatability with mikmod, this will mean all mod loaders and perfect sound recreation.

 

Future Improvements

Plan to move the library to a move OOP programming style and clean up the code.

Create exceptions for the different fail reasons to make it easer to know why a mod load failed.

 

Current Support level

Supported Loaders

        Mod
        M15
        669
        XM
        IT
        S3M
        FAR
        ULT
        MTM 

     

    Supported Drivers

        NAudio
        Wav writer
        No Audio
        XNA Dynamic Sound Effect Driver (PC, Xbox 360, WP7) 

    Supported Mixers

        LQ Software Mixer
        HQ Software Mixer (WIP) 

 

 

Tools

To aid in the goals a unit test like tool has been created that will run over a selection of files and get the C version of the lib to create a wav file and then get SharpMod todo the same.  the resulting errors and wav files will then be compared to make sure the end results match.
