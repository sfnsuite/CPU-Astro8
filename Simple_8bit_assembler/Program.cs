﻿
//Microsoft (R) Visual C# Compiler version 3.4.0-beta4-19562-05 (ff930dec)
//Copyright (C) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class Program
{
    static string[] instructions = { "NOP", "LODA", "LODB", "ADD", "SUB", "OUT", "JMP", "STA", "LDI", "JMPZ", "JMPC", "HLT", "LDAIN", "", "", "" };

    public static void Main(string[] args)
    {
        Console.Write("Action >  ");
        string action = Console.ReadLine().ToLower();

        if (action != "microcode" & action != "emulator")
        {
            Console.Write("v Code input v\n");
            string code = "";
            string line;
            while (!String.IsNullOrWhiteSpace(line = Console.ReadLine()))
            {
                code += line + "\n";
            }

            List<string> outputBytes = parseCode(code);

            Console.Write("\n\n");

            string processedOutput = "";
            // Print the output
            Console.Write("\nv3.0 hex words addressed\n");
            processedOutput += "\nv3.0 hex words addressed\n";
            Console.Write("000: ");
            processedOutput += "000: ";
            for (int outindex = 0; outindex < outputBytes.Count; outindex++)
            {
                if (outindex % 8 == 0 && outindex != 0)
                {
                    Console.Write("\n" + DecToHexFilled(outindex, 3) + ": ");
                    processedOutput += "\n" + DecToHexFilled(outindex, 3) + ": ";
                }
                Console.Write(outputBytes[outindex] + " ");
                processedOutput += outputBytes[outindex] + " ";
            }

            File.WriteAllText("../../../../program_machine_code", processedOutput);
        }
        else if (action != "emulator")
        {
            // Generate zeros in data
            string[] output = new string[1024];
            for (int osind = 0; osind < output.Length; osind++) { output[osind] = "00000"; }

            string[] microinstructions = { "SU", "IW", "DW", "ST", "CE", "CR", "WM", "RA", "EO", "FL", "J", "WB", "WA", "RM", "AW", "IR", "EI" };
            string[] flags = { "ZEROFLAG", "CARRYFLAG" };
            string[] instructioncodes = {
                "fetch( 0=aw,cr & 1=rm,iw,ce & 2=ei", // Fetch
                "loda( 2=aw,ir & 3=wa,rm & 4=ei", // LoadA
                "lodb( 2=aw,ir & 3=wb,rm & 4=ei", // LoadB
                "add( 2=aw,ir & 3=wb,rm & 4=wa,eo,fl & 5=ei", // Add <addr>
                "sub( 2=aw,ir & 3=wb,rm & 4=wa,eo,su,fl & 5=ei", // Subtract <addr>
                "out( 2=ra,dw & 3=ei", // Output to decimal display and LCD screen
                "jmp( 2=ir,j & 3=ei", // Jump <addr>
                "sta( 2=aw,ir & 3=ra,wm & 4=ei", // Store A <addr>
                "ldi( 2=wa,ir & 3=ei", // Load immediate A <val>
                "jmpz( 2=ir,j | zeroflag & 3=ei", // Jump if zero <addr>
                "jmpc( 2=ir,j | carryflag & 3=ei", // Jump if carry <addr>
                "hlt( 2=st & 3=ei", // Stop the computer clock
                "ldain( 2=ra,aw & 3=wa,rm & 4=ei", // Load from reg A as memory address, then copy value from memory into A
            };
            
            // Remove spaces from instruction codes and make uppercase
            for (int cl = 0; cl < instructioncodes.Length; cl++)
            {
                string newStr = "";
                for (int clc = 0; clc < instructioncodes[cl].Length; clc++)
                {
                    if (instructioncodes[cl][clc] != ' ')
                        newStr += instructioncodes[cl][clc];
                }
                Console.WriteLine(newStr.ToUpper());
                instructioncodes[cl] = newStr.ToUpper();
            }
            
            // Create indexes for instructions, which allows for duplicates to execute differently for different parameters
            int[] instIndexes = new int[instructioncodes.Length];
            List<string> seenNames = new List<string>();
            for (int cl = 0; cl < instructioncodes.Length; cl++)
            {
                string instName = instructioncodes[cl].Split('(')[0];
                bool foundInList = false;
                for (int clc = 0; clc < seenNames.Count; clc++)
                {
                    if (instName == seenNames[clc])
                    {
                        instIndexes[cl] = clc;
                        foundInList = true;
                        break;
                    }
                }
                if(!foundInList){
                    seenNames.Add(instName);
                    instIndexes[cl] = seenNames.Count-1;
                }
                instructioncodes[cl] = instructioncodes[cl].Split('(')[1];
            }

            // Special process fetch instruction
            Console.WriteLine("\n"+instructioncodes[0]);
            for (int ins = 0; ins < instructioncodes.Length; ins++) // Iterate through all definitions of instructions
            {
                int correctedIndex = instIndexes[ins];
                
                string startaddress = DecToBinFilled(correctedIndex, 4);

                string[] instSteps = instructioncodes[0].Split('&');
                for (int step = 0; step < instSteps.Length; step++) // Iterate through every step
                {
                    int actualStep = int.Parse(instSteps[step].Split('=')[0]);
                    string stepContents = instSteps[step].Split('=')[1].Split('|')[0];

                    string midaddress = DecToBinFilled(actualStep, 4);

                    string stepComputedInstruction = "";
                    for (int mins = 0; mins < microinstructions.Length; mins++)
                    {
                        if (stepContents.Contains(microinstructions[mins]))
                            stepComputedInstruction += "1";
                        else
                            stepComputedInstruction += "0";
                    }

                    // Compute flags combinations
                    for (int flagcombinations = 0; flagcombinations < flags.Length*flags.Length; flagcombinations++)
                    {
                        char[] endaddress = { '0', '0' };
                        // Look for flags
                        if (instSteps[step].Contains("|"))
                        {
                            string[] inststepFlags = instSteps[step].Split('|')[1].Split(',');
                            for (int flag = 0; flag < inststepFlags.Length; flag++) // Iterate through all flags in step
                            {
                                for (int checkflag = 0; checkflag < flags.Length; checkflag++) // What is the index of the flag
                                {
                                    if (inststepFlags[flag] == flags[checkflag])
                                        endaddress[checkflag] = '1';
                                }
                            }
                        }
                        char[] newendaddress = DecToBinFilled(flagcombinations, 2).ToCharArray();

                        bool doesntmatch = false;
                        for (int i = 0; i < endaddress.Length; i++)
                        {
                            if (endaddress[i] == '1')
                            {
                                if (newendaddress[i] != '1')
                                    doesntmatch = true;
                            }
                        }
                        if (doesntmatch)
                            continue;

                        Console.WriteLine("\t& " + startaddress + " " + midaddress + " " + new string(newendaddress) + "  =  " + BinToHexFilled(stepComputedInstruction, 4));
                        output[BinToDec(startaddress + midaddress + new string(newendaddress))] = BinToHexFilled(stepComputedInstruction, 5);
                    }
                }

                //Console.WriteLine();
            }

            // Do actual processing
            for (int ins = 1; ins < instructioncodes.Length; ins++) // Iterate through all definitions of instructions
            {
                int correctedIndex = instIndexes[ins];
                
                Console.WriteLine(instructioncodes[correctedIndex]);

                string startaddress = DecToBinFilled(correctedIndex, 4);

                string[] instSteps = instructioncodes[correctedIndex].Split('&');
                for (int step = 0; step < instSteps.Length; step++) // Iterate through every step
                {
                    int actualStep = int.Parse(instSteps[step].Split('=')[0]);
                    string stepContents = instSteps[step].Split('=')[1].Split('|')[0];

                    string midaddress = DecToBinFilled(actualStep, 4);

                    string stepComputedInstruction = "";
                    for (int mins = 0; mins < microinstructions.Length; mins++)
                    {
                        if (stepContents.Contains(microinstructions[mins]))
                            stepComputedInstruction += "1";
                        else
                            stepComputedInstruction += "0";
                    }

                    // Compute flags combinations
                    for (int flagcombinations = 0; flagcombinations < flags.Length * flags.Length; flagcombinations++)
                    {
                        char[] endaddress = { '0', '0' };
                        int[] stepLocked = { 0, 0 };
                        // If flags are specified in current step layer, set them to what is specified and lock that bit
                        if (instSteps[step].Contains("|"))
                        {
                            string[] inststepFlags = instSteps[step].Split('|')[1].Split(',');
                            for (int flag = 0; flag < inststepFlags.Length; flag++) // Iterate through all flags in step
                            {
                                for (int checkflag = 0; checkflag < flags.Length; checkflag++) // What is the index of the flag
                                {
                                    if (inststepFlags[flag].Contains(flags[checkflag])){
                                        if(inststepFlags[flag][0] == '!')
                                            endaddress[checkflag] = '0';
                                        else
                                            endaddress[checkflag] = '1';
                                        stepLocked[checkflag] = 1;
                                    }
                                }
                            }
                        }
                        char[] newendaddress = DecToBinFilled(flagcombinations, 2).ToCharArray();

                        // Make sure the current combination doesn't change the locked bits, otherwise go to next step
                        bool doesntmatch = false;
                        for (int i = 0; i < endaddress.Length; i++)
                        {
                            if (stepLocked[i] == 1)
                            {
                                if (newendaddress[i] != endaddress[i])
                                    doesntmatch = true;
                            }
                        }
                        if (doesntmatch)
                            continue;

                        Console.WriteLine("\t& " + startaddress + " " + midaddress + " " + new string(newendaddress) + "  =  " + BinToHexFilled(stepComputedInstruction, 5));
                        output[BinToDec(startaddress + midaddress + new string(newendaddress))] = BinToHexFilled(stepComputedInstruction, 5);
                    }
                }

                //Console.WriteLine();
            }


            string processedOutput = "";

            // Print the output
            Console.Write("\nv3.0 hex words addressed\n");
            processedOutput += "\nv3.0 hex words addressed\n";
            Console.Write("000: ");
            processedOutput += "000: ";
            for (int outindex = 0; outindex < output.Length; outindex++)
            {
                if (outindex % 8 == 0 && outindex != 0)
                {
                    Console.Write("\n" + DecToHexFilled(outindex, 3) + ": ");
                    processedOutput += "\n" + DecToHexFilled(outindex, 3) + ": ";
                }
                Console.Write(output[outindex] + " ");
                processedOutput += output[outindex] + " ";
            }

            File.WriteAllText("../../../../microinstructions_cpu_v1", processedOutput);
        }
        else
        {
            int su, iw, dw, st, ce, cr, wm, ra, eo, fl, j, wb, wa, rm, aw, ir, ei = 0;
            int AReg = 0;
            int BReg = 0;
            int InstructionReg = 0;
            int[] flags = {0, 0, 0};
            int bus = 0;
            int outputReg = 0;
            int memoryIndex = 0;
			int programCounter = 0;
            
            Console.Write("v Emu. Code input v\n");
            string code = "";
            string line;
            while (!String.IsNullOrWhiteSpace(line = Console.ReadLine())) { code += line + "\n"; }
            List<string> outputBytes = parseCode(code);
            
            while(true)
            {
                for(int step = 0; step < 16; step++)
                {
					// Fetch
            	    if(step == 0){
						su = iw = dw = st = ce = cr = wm = ra = eo = fl = j = wb = wa = rm = aw = ir = ei = 0;
						cr,= aw = 1;
					}
            	    if(step == 1){
						su = iw = dw = st = ce = cr = wm = ra = eo = fl = j = wb = wa = rm = aw = ir = ei = 0;
						iw = ce = rm = ei = 1;
					}
            	    
            	    // iw, dw, st, ce, cr, wm, ra, fl, j, wb, wa, rm, aw, ir, ei
            	    // Execute microinstructions
            	    if (eo == 1)
            	    {
            	        if(su == 1)
            	            AReg = AReg - BReg;
            	        else
            	            AReg = AReg + BReg;
            	    }
            	    if (ei == 1)
					{
						
					}
                }
            }
        }
        
        Console.WriteLine("\n");
        Console.ReadLine();
    }

    static string DecToHexFilled(int input, int desiredSize)
    {
        string output = input.ToString("X");

        while (output.Length < desiredSize)
        {
            output = "0" + output;
        }

        return output;
    }
    static string BinToHexFilled(string input, int desiredSize)
    {
        string output = Convert.ToInt32(input, 2).ToString("X");

        while (output.Length < desiredSize)
        {
            output = "0" + output;
        }

        return output;
    }
    static int BinToDec(string input)
    {
        int output = Convert.ToInt32(input, 2);
        return output;
    }
    static string DecToBinFilled(int input, int desiredSize)
    {
        string output = Convert.ToString(input, 2);

        while (output.Length < desiredSize)
        {
            output = "0" + output;
        }

        return output;
    }
    List<string> parseCode(string input)
    {
            List<string> outputBytes = new List<string>();
            for (int i = 0; i < 4000; i++)
                outputBytes.Add("0000");

            string[] splitcode = input.ToUpper().Split('\n');

            int memaddr = 0;
            for (int i = 0; i < splitcode.Length; i++)
            {
                if (splitcode[i] == null || splitcode[i] == "")
                {
                    continue;
                }

                string[] splitBySpace = splitcode[i].Split(' ');

                if (splitBySpace[0][0] == ',')
                {
                    Console.Write("-\t"+splitcode[i] + "\n");
                    continue;
                }
                if (splitBySpace[0] == "SET")
                {
                    string hVal = DecToHexFilled(Int32.Parse(splitBySpace[2]), 4);
                    outputBytes[Int32.Parse(splitBySpace[1])] = hVal;
                    Console.Write("-\t" + splitcode[i] + "\t  ~   ~\n");
                    continue;
                }

                Console.Write(memaddr + " " + splitcode[i] + "   \t  =>  ");

                // Find index of instruction
                for (int f = 0; f < instructions.Length; f++)
                {
                    if (instructions[f] == splitBySpace[0])
                    {
                        Console.Write(DecToHexFilled(f, 1));
                        outputBytes[memaddr] = DecToHexFilled(f, 1);
                    }
                }

                // Check if any args are after the command
                if (splitcode[i] != splitBySpace[0])
                {
                    Console.Write(DecToHexFilled(Int32.Parse(splitBySpace[1]), 3));
                    outputBytes[memaddr] += DecToHexFilled(Int32.Parse(splitBySpace[1]), 3);
                }
                else
                {
                    Console.Write("0");
                    outputBytes[memaddr] += "000";
                }
                Console.Write("\n");
                memaddr++;
            }
        return outputBytes;
    }
}
