using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class CSIMScript : MonoBehaviour
{
	public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
	
	public KMSelectable[] Numbers;
	public KMSelectable Negate, Submit;
	public AudioClip[] SFX;
	
	public TextMesh Input, Output, Display;
	public TextMesh[] Checkmarks;
	
	string[] Operators = {"+", "-", "*"}, RuleLetter = {"A", "B", "C", "F", "I", "L", "N", "O", "P", "S", "X", "Z"};
	int StageNumber = 0, ColorOperator = 0, PreviousNumberTracker = 0;
	int FirstInput = 0, FirstOutput = 0, PreviousOutput = 0, PreviousInput = 0, PreviousColor = 11, Answer = -1, InputNumber = 0, FactorNumber = 0;
	string InputDisplay = "", DisplayDisplay = "", Equation = "";
	bool Interactable = true, Inverted = false, Looping = false;
	Coroutine ColorLoop;
	
	// Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int i = 0; i < Numbers.Length; i++)
		{
			int Press = i;
			Numbers[i].OnInteract += delegate ()
			{
				PressNumber(Press);
				return false;
			};
		}
		Negate.OnInteract += delegate () {Negator(); return false; };
		Submit.OnInteract += delegate () {Submitting(); return false; };
	}
	
	void Start()
	{
		GenerateAnswer();
		Input.text = InputDisplay;
		Display.text = DisplayDisplay;
		Checkmarks[StageNumber].text = "?";
	}
	
	void PressNumber(int Press)
	{
		if (Interactable)
		{
			Numbers[Press].AddInteractionPunch(0.2f);
			Audio.PlaySoundAtTransform(SFX[0].name, transform);
			Output.text = Output.text[1].ToString() + Press.ToString();
		}
	}
	
	void Submitting()
	{
		if (Interactable)
		{
			Submit.AddInteractionPunch(0.2f);
			if (Output.text[0].ToString() != "-")
			{
				Interactable = false;
				if (Looping)
				{
					Looping = false;
					StopCoroutine(ColorLoop);
				}
				int GeneratedOutput = Inverted ? Int32.Parse(Output.text) * -1 : Int32.Parse(Output.text);
				StartCoroutine(Processor(GeneratedOutput));
				Audio.PlaySoundAtTransform(SFX[2].name, transform);
			}
			
			else
			{
				Audio.PlaySoundAtTransform(SFX[0].name, transform);
			}
		}
	}
	
	void Negator()
	{
		if (Interactable)
		{
			Negate.AddInteractionPunch(0.2f);
			Audio.PlaySoundAtTransform(SFX[1].name, transform);
			Inverted = Inverted == true ? false : true;
			Output.color = Inverted ? new Color32(255,0,0,255) : new Color32(255,255,255,255);
		}
	}
	
	void GenerateAnswer()
	{
		Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] ------------------------------------------", moduleId);
		Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] Current Stage: {1}", moduleId, (StageNumber+1).ToString());
		InputNumber = UnityEngine.Random.Range(0,100);
		InputDisplay = InputNumber.ToString().Length == 1 ? "0" + InputNumber.ToString() : InputNumber.ToString();
		InputNumber = UnityEngine.Random.Range(0,2) == 0 ? InputNumber : InputNumber * -1;
		Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] Generated Input: {1}", moduleId, InputNumber.ToString());
		Input.color = InputNumber < 0 ? new Color32(128, 0, 0, 255) : new Color32(128, 128, 128, 255);
		DisplayDisplay = Operators[UnityEngine.Random.Range(0,Operators.Length)] + RuleLetter[UnityEngine.Random.Range(0,RuleLetter.Length)];
		Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] Function Display: {1}", moduleId, DisplayDisplay);
		ColorOperator = UnityEngine.Random.Range(0, UnityEngine.Random.Range(0,10) < 4 ? 6 : 12);
		string ColorPicked = "";
		
		switch (ColorOperator)
		{
			case 0:
				Display.color = new Color32(255,0,0,255);
				ColorPicked = "Red";
				break;
			case 1:
				Display.color = new Color32(0,255,255,255);
				ColorPicked = "Cyan";
				break;
			case 2:
				Display.color = new Color32(0,255,0,255);
				ColorPicked = "Green";
				break;
			case 3:
				Display.color = new Color32(255,0,255,255);
				ColorPicked = "Magenta";
				break;
			case 4:
				Display.color = new Color32(0,0,255,255);
				ColorPicked = "Blue";
				break;
			case 5:
				Display.color = new Color32(255,255,0,255);
				ColorPicked = "Yellow";
				break;
			case 6:
				Display.color = new Color32(128,0,0,255);
				ColorPicked = "Dark Red";
				break;
			case 7:
				Display.color = new Color32(0,128,0,255);
				ColorPicked = "Dark Green";
				break;
			case 8:
				Display.color = new Color32(0,0,128,255);
				ColorPicked = "Dark Blue";
				break;
			case 9:
				ColorLoop = StartCoroutine(ColorCycle());
				ColorPicked = "Multicolored";
				break;
			case 10:
				Display.color = new Color32(255,255,255,255);
				ColorPicked = "White";
				break;
			default:
				Display.color = new Color32(128,128,128,255);
				ColorPicked = "Gray";
				break;
		}
		
		Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] Display Color: {1}", moduleId, ColorPicked);
		
		GenerateInput();
		
		switch(DisplayDisplay[1].ToString())
		{
			case "A":
				FactorNumber = StageNumber;
				break;
			case "B":
				FactorNumber = Bomb.GetBatteryCount();
				break;
			case "C":
				FactorNumber = InputNumber;
				break;
			case "F":
				FactorNumber = FirstOutput;
				break;
			case "I":
				FactorNumber = FirstInput;
				break;
			case "L":
				int InputNumberProcessed = InputNumber < 0 ? InputNumber * 1 : InputNumber;
				switch(InputNumberProcessed)
				{
					case 0:
					case 1:
						FactorNumber = -9999;
						break;
					default:
						int[] Primes = {97, 89, 83, 79, 73, 71, 67, 61, 59, 53, 47, 43, 41, 37, 31, 29, 23, 19, 17, 13, 11, 7, 5, 3, 2};
						for (int x = 0; x < Primes.Length; x++)
						{
							if (InputNumberProcessed % Primes[x] == 0)
							{
								FactorNumber = Primes[x];
								break;
							}
						}
						break;
				}
				break;
			case "N":
				FactorNumber = Bomb.GetSerialNumberNumbers().Last();
				break;
			case "O":
				FactorNumber = PreviousOutput;
				break;
			case "P":
				FactorNumber = PreviousInput;
				break;
			case "S":
				FactorNumber = Bomb.GetSerialNumberNumbers().Sum();
				break;
			case "X":
				FactorNumber = Bomb.GetStrikes();
				break;
			case "Z":
				string Serial =  Bomb.GetSerialNumber();
				FactorNumber = Int32.Parse(Serial[2].ToString() + Serial[5].ToString());
				break;
			default:
				break;
		}
		
		switch (ColorOperator)
		{
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
				switch(DisplayDisplay[0].ToString())
				{
					case "+":
						Answer = ColorOperator%2 == 0 ? InputNumber + PreviousOutput : InputNumber + FirstInput;
						Equation = ColorOperator%2 == 0 ? "(" + InputNumber.ToString() + " + " + PreviousOutput.ToString() + ")" : "(" + InputNumber.ToString() + " + " + FirstInput.ToString() + ")";
						break;
					case "-":
						Answer = ColorOperator%2 == 0 ? InputNumber - PreviousOutput : InputNumber - FirstInput;
						Equation = ColorOperator%2 == 0 ? "(" + InputNumber.ToString() + " - " + PreviousOutput.ToString() + ")" : "(" + InputNumber.ToString() + " - " + FirstInput.ToString() + ")";
						break;
					case "*":
						Answer = ColorOperator%2 == 0 ? InputNumber * PreviousOutput : InputNumber * FirstInput;
						Equation = ColorOperator%2 == 0 ? "(" + InputNumber.ToString() + " * " + PreviousOutput.ToString() + ")" : "(" + InputNumber.ToString() + " * " + FirstInput.ToString() + ")";
						break;
					default:
						break;
				}
				
				switch (ColorOperator/2)
				{
					case 0:
						Answer = FactorNumber == -9999 ? Answer * 1 : Answer * FactorNumber;
						Equation = FactorNumber == -9999 ? Equation : Equation + " * " + FactorNumber.ToString();
						break;
					case 1:
						Answer = FactorNumber == -9999 ? Answer + 0 : Answer + FactorNumber;
						Equation = FactorNumber == -9999 ? Equation : Equation + " + " + FactorNumber.ToString();
						break;
					default:
						Answer = FactorNumber == -9999 ? Answer - 0 : Answer - FactorNumber;
						Equation = FactorNumber == -9999 ? Equation : Equation + " - " + FactorNumber.ToString();
						break;
				}
				break;
			case 6:
				Answer = FactorNumber == -9999 ? InputNumber + 0 : InputNumber + FactorNumber;
				Equation = FactorNumber == -9999 ? InputNumber.ToString() + " + 0" : InputNumber.ToString() + " + " + FactorNumber.ToString();
				break;
			case 7:
				Answer = FactorNumber == -9999 ? InputNumber - 0 : InputNumber - FactorNumber;
				Equation = FactorNumber == -9999 ? InputNumber.ToString() + " - 0" : InputNumber.ToString() + " - " + FactorNumber.ToString();
				break;
			case 8:
				Answer = FactorNumber == -9999 ? InputNumber * 1 : InputNumber * FactorNumber;
				Equation = FactorNumber == -9999 ? InputNumber.ToString() + " * 1" : InputNumber.ToString() + " * " + FactorNumber.ToString();
				break;
			case 9:
				Answer = FactorNumber == -9999 ? 0 : FactorNumber;
				Equation = Answer.ToString();
				break;
			case 10:
				Answer = NestedCommand(PreviousColor);
				break;
			case 11:
				Answer = InputNumber;
				Equation = Answer.ToString();
				break;
			default:
				break;
		}
		Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] Formula: {1}", moduleId, Equation);
		
		if (Answer > 0 && Answer > 99)
		{
			while (Answer > 99)
			{
				Answer -= 100;
			}
		}
		
		else if (Answer < 0 && Answer < -99)
		{
			while (Answer <- 99)
			{
				Answer += 100;
			}
		}
		PreviousNumberTracker = InputNumber;
		Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] Correct Answer: {1}", moduleId, Answer.ToString());
	}
	
	void GenerateInput()
	{
		string Serial =  Bomb.GetSerialNumber();
		switch (StageNumber)
		{
			case 0:
				FirstInput = InputNumber;
				FirstOutput = Int32.Parse(Serial[2].ToString() + Serial[5].ToString());
				PreviousInput = Int32.Parse(Serial[2].ToString() + Serial[5].ToString());
				PreviousOutput = Int32.Parse(Serial[2].ToString() + Serial[5].ToString());
				break;
			case 1:
				FirstOutput = Answer;
				PreviousInput = FirstInput;
				PreviousOutput = Answer;
				break;
			case 2:
			case 3:
				PreviousOutput = Answer;
				PreviousInput = PreviousNumberTracker;
				break;
			default:
				break;
		}
	}
	
	int NestedCommand(int BaseColor)
	{
		int Basis = 0;
		switch (BaseColor)
		{
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
				switch(DisplayDisplay[0].ToString())
				{
					case "+":
						Basis = ColorOperator%2 == 0 ? InputNumber + PreviousOutput : InputNumber + FirstInput;
						Equation = ColorOperator%2 == 0 ? "(" + InputNumber.ToString() + " + " + PreviousOutput.ToString() + ")" : "(" + InputNumber.ToString() + " + " + FirstInput.ToString() + ")";
						break;
					case "-":
						Basis = ColorOperator%2 == 0 ? InputNumber - PreviousOutput : InputNumber - FirstInput;
						Equation = ColorOperator%2 == 0 ? "(" + InputNumber.ToString() + " - " + PreviousOutput.ToString() + ")" : "(" + InputNumber.ToString() + " - " + FirstInput.ToString() + ")";
						break;
					case "*":
						Basis = ColorOperator%2 == 0 ? InputNumber * PreviousOutput : InputNumber * FirstInput;
						Equation = ColorOperator%2 == 0 ? "(" + InputNumber.ToString() + " * " + PreviousOutput.ToString() + ")" : "(" + InputNumber.ToString() + " * " + FirstInput.ToString() + ")";
						break;
					default:
						break;
				}
				
				switch (ColorOperator/2)
				{
					case 0:
						Basis = FactorNumber == -9999 ? Basis * 1 : Basis * FactorNumber;
						Equation = FactorNumber == -9999 ? Equation : Equation + " * " + FactorNumber.ToString();
						break;
					case 1:
						Basis = FactorNumber == -9999 ? Basis + 0 : Basis + FactorNumber;
						Equation = FactorNumber == -9999 ? Equation : Equation + " + " + FactorNumber.ToString();
						break;
					default:
						Basis = FactorNumber == -9999 ? Basis - 0 : Basis - FactorNumber;
						Equation = FactorNumber == -9999 ? Equation : Equation + " - " + FactorNumber.ToString();
						break;
				}
				break;
			case 6:
				Basis = FactorNumber == -9999 ? InputNumber + 0 : InputNumber + FactorNumber;
				Equation = FactorNumber == -9999 ? InputNumber.ToString() + " + 0" : InputNumber.ToString() + " + " + FactorNumber.ToString();
				break;
			case 7:
				Basis = FactorNumber == -9999 ? InputNumber - 0 : InputNumber - FactorNumber;
				Equation = FactorNumber == -9999 ? InputNumber.ToString() + " - 0" : InputNumber.ToString() + " - " + FactorNumber.ToString();
				break;
			case 8:
				Basis = FactorNumber == -9999 ? InputNumber * 1 : InputNumber * FactorNumber;
				Equation = FactorNumber == -9999 ? InputNumber.ToString() + " * 1" : InputNumber.ToString() + " * " + FactorNumber.ToString();
				break;
			case 9:
				Basis = FactorNumber == -9999 ? 0 : FactorNumber;
				Equation = Basis.ToString();
				break;
			case 10:
			case 11:
				Basis = InputNumber;
				Equation = Basis.ToString();
				break;
			default:
				break;
		}
		return Basis;
	}
	
	IEnumerator ColorCycle()
    {
		Looping = true;
		Color32[] ColorCycle = {new Color32(255,0,0,255), new Color32(255,127,0,255), new Color32(255,255,0,255), new Color32(127,255,0,255), new Color32(0,255,0,255), new Color32(0,255,127,255), new Color32(0,255,255,255), new Color32(0,127,255,255), new Color32(0,0,255,255), new Color32(127,0,255,255), new Color32(255,0,255,255), new Color32(255,0,127,255)};
		int CurrentNumber = UnityEngine.Random.Range(0,ColorCycle.Length), NextNumber = (CurrentNumber + 1)%ColorCycle.Length;
		while (true)
		{
			var elapsed = 0f;
			var duration = 1f;
			while (elapsed < duration)
			{
				Display.color = Color.Lerp(ColorCycle[CurrentNumber], ColorCycle[NextNumber], elapsed / duration);
				yield return null;
				elapsed += Time.deltaTime;
			}
			Display.color = ColorCycle[NextNumber];
			CurrentNumber = (CurrentNumber + 1)%ColorCycle.Length;
			NextNumber = (NextNumber + 1)%ColorCycle.Length;
		}
	}
	
	IEnumerator Processor(int TheAnswer)
	{
		Input.text = Output.text = Display.text = Checkmarks[StageNumber].text = "";
		yield return new WaitForSecondsRealtime(1f);
		for (int x = 0; x < 3; x++)
		{
			Checkmarks[StageNumber].text = Checkmarks[StageNumber].text + ".";
			Audio.PlaySoundAtTransform(SFX[3].name, transform);
			yield return new WaitForSecondsRealtime(1f);
		}
		if (TheAnswer == Answer)
		{
			if (StageNumber == 3)
			{
				Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] You sent: {1}. That was correct. The module is being solved.", moduleId, TheAnswer.ToString());
				string Done = "DONE";
				for (int x = 0; x < 4; x++)
				{
					Checkmarks[3-x].text = Done[x].ToString();
					Audio.PlaySoundAtTransform(SFX[8].name, transform);
					yield return new WaitForSecondsRealtime(.25f);
				}
				yield return new WaitForSecondsRealtime(.5f);
				Output.color = Input.color = Display.color = new Color32(0,255,0,255);
				for (int x = 0; x < 6; x++)
				{
					if (x < 2)
					{
						Input.text = Input.text + "G";
						Audio.PlaySoundAtTransform(SFX[9+(x%2)].name, transform);
						yield return new WaitForSecondsRealtime(x%2 == 0 ? .2f : .3f);
					}
					
					else if (x < 4)
					{
						Output.text = Output.text + "G";
						Audio.PlaySoundAtTransform(SFX[9+(x%2)].name, transform);
						yield return new WaitForSecondsRealtime(x%2 == 0 ? .2f : .3f);
					}
					
					else
					{
						Display.text = Display.text + "G";
						Audio.PlaySoundAtTransform(SFX[x % 2 == 0 ? 9 : 11].name, transform);
						yield return new WaitForSecondsRealtime(x%2 == 0 ? .2f : 0f);
					}
				}
				Module.HandlePass();
			}
			
			else
			{
				Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] You sent: {1}. That was correct. The module is proceeding to the next stage.", moduleId, TheAnswer.ToString());
				Audio.PlaySoundAtTransform(SFX[4].name, transform);
				Checkmarks[StageNumber].text = "!";
				StageNumber++;
				Checkmarks[StageNumber].text = "?";
				yield return new WaitForSecondsRealtime(1.5f);
				PreviousColor = ColorOperator;
				GenerateAnswer();
				Inverted = false;
				Output.color = new Color32(255,255,255,255);
				for (int x = 0; x < 6; x++)
				{
					if (x < 2)
					{
						Input.text = Input.text + InputDisplay[x%2].ToString();
					}
					
					else if (x < 4)
					{
						Output.text = Output.text + "-";
					}
					
					else
					{
						Display.text = Display.text + DisplayDisplay[x%2].ToString();
					}
					Audio.PlaySoundAtTransform(SFX[5].name, transform);
					yield return new WaitForSecondsRealtime(.25f);
				}
				Interactable = true;
			}
		}
		
		else
		{
			Debug.LogFormat("[Color-Symbolic Interpretation Module #{0}] You sent: {1}. That was incorrect. The module is going to reset back to Stage 1.", moduleId, TheAnswer.ToString());
			for (int x = 0; x < Checkmarks.Length; x++)
			{
				Checkmarks[x].text = "X";
			}
			Audio.PlaySoundAtTransform(SFX[6].name, transform);
			yield return new WaitForSecondsRealtime(1.5f);
			Module.HandleStrike();
			yield return new WaitForSecondsRealtime(.5f);
			StageNumber = 0;
			for (int x = 0; x < Checkmarks.Length; x++)
			{
				Checkmarks[x].text = "-";
				Audio.PlaySoundAtTransform(SFX[7].name, transform);
				yield return new WaitForSecondsRealtime(.5f);
			}
			Audio.PlaySoundAtTransform(SFX[7].name, transform);
			Checkmarks[StageNumber].text = "?";
			yield return new WaitForSecondsRealtime(1f);
			GenerateAnswer();
			PreviousColor = 11;
			Inverted = false;
			Output.color = new Color32(255,255,255,255);
			for (int x = 0; x < 6; x++)
			{
				if (x < 2)
				{
					Input.text = Input.text + InputDisplay[x%2].ToString();
				}
				
				else if (x < 4)
				{
					Output.text = Output.text + "-";
				}
				
				else
				{
					Display.text = Display.text + DisplayDisplay[x%2].ToString();
				}
				Audio.PlaySoundAtTransform(SFX[5].name, transform);
				yield return new WaitForSecondsRealtime(.25f);
			}
			Interactable = true;
		}
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To submit your answer on the module, use the command !{0} submit [-99,99]";
    #pragma warning restore 414
	
	IEnumerator ProcessTwitchCommand(string command)
    {
		string[] parameters = command.Split(' ');
		if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (!Interactable)
			{
				yield return "sendtochaterror You are unable to interact with the module currently. The command was not processed.";
				yield break;
			}
			
			if (parameters.Length != 2)
			{
				yield return "sendtochaterror Invalid parameter length. The command was not processed.";
				yield break;
			}
			
			int Out;
			if (!Int32.TryParse(parameters[1], out Out))
			{
				yield return "sendtochaterror The submitted number can not be processed. The command was not processed.";
				yield break;
			}
			
			if (Out > 99 || Out < -99)
			{
				yield return "sendtochaterror The submitted number was not in range of -99 to 99. The command was not processed.";
				yield break;
			}
			
			if ((Out < 0 && !Inverted) || (Out > 0 && Inverted))
			{
				Negate.OnInteract();
				
			}
			yield return new WaitForSecondsRealtime(.1f);
			
			Out = Out < 0 ? Out * -1 : Out;
			string Converted = Out.ToString();
			
			for (int x = 0; x < Converted.Length; x++)
			{
				if (Out.ToString().Length == 1)
				{
					Numbers[0].OnInteract();
					yield return new WaitForSecondsRealtime(.1f);
				}
				
				Numbers[Int32.Parse(Converted[x].ToString())].OnInteract();
				yield return new WaitForSecondsRealtime(.1f);
			}
			
			yield return "strike";
			yield return "solve";
			Submit.OnInteract();
			yield return new WaitForSecondsRealtime(.1f);
		}
	}
}