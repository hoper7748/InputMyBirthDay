using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AgeVerification : MonoBehaviour
{
    // -- Dialog Enums ---------------------------------------------------------
    public enum DialogType
    {
        None,
        YearDialog,
        MonthAndDayDialog,
        DeniedDialog,
        ConfirmationDialog
    }

    public enum AgeVerificationState
    {
        DateOfBirthLessThan18,
        DateOfBirthGreaterThan18,
        EnteringBirthYear,
        ConfirmingBirthYear,
        EnteringBirthMonthAndDay,
        ConfirmingBirthMonthAndDay
    }

    // -- Inspector References --------------------------------------------------
    [Header("Dialog GameObjects")]
    public GameObject YearDialog;
    public GameObject MonthAndDayDialog;
    public GameObject DeniedDialog;
    public GameObject ConfirmationDialog;

    [Header("Year Dialog UI")]
    public TMP_Text YearTextField;
    [Tooltip("Buttons for digits 0–9")]
    public Button[] NumberButtons;      // Size = 10 in the Inspector
    public Button DeleteButton;

    [Header("Month/Day Dialog UI")]
    public TMP_Dropdown MonthDropdown;
    public TMP_Dropdown DayDropdown;
    public Button MonthDaySubmitButton;

    [Header("Confirmation Dialog UI")]
    public TMP_Text ConfirmationMessage;
    public Button ConfirmButton;
    public Button EditButton;

    // -- Internal Data ---------------------------------------------------------
    private DateTime _dateOfBirth = default;  // The actual DOB
    private AgeVerificationState _currentState;
    private string _yearInput = "";           // For capturing the 4-digit year

    // --------------------------------------------------------------------------
    private void Awake()
    {
        // Wire up number buttons to OnNumberButtonClicked
        // Index-based approach: button 0 => "0", button 1 => "1", etc.
        for (int i = 0; i < NumberButtons.Length; i++)
        {
            int number = i;
            NumberButtons[i].onClick.AddListener(() => OnNumberButtonClicked(number.ToString()));
        }

        // Wire up delete button
        DeleteButton.onClick.AddListener(OnDeleteButtonClicked);

        // Wire up other buttons
        MonthDaySubmitButton.onClick.AddListener(OnMonthDaySubmit);
        ConfirmButton.onClick.AddListener(OnConfirmDialogConfirm);
        EditButton.onClick.AddListener(OnConfirmDialogEdit);
    }

    private void Start()
    {
        // 1) Attempt to load a saved DOB.
        DateTime savedDate = GetSavedDateOfBirth();
        if (savedDate != default)
        {
            // We have a saved DOB; check if user is >=18
            if (IsOlderThan18(savedDate))
            {
                _currentState = AgeVerificationState.DateOfBirthGreaterThan18;
                ShowDialog(DialogType.None); // Hide all if user is already verified
            }
            else
            {
                _currentState = AgeVerificationState.DateOfBirthLessThan18;
                ShowDialog(DialogType.DeniedDialog); // Denied if < 18
            }
            return;
        }

        // 2) If no saved DOB, start entering birth year
        _currentState = AgeVerificationState.EnteringBirthYear;
        ResetYearInput();
        ShowDialog(DialogType.YearDialog);
    }

    // -- Example placeholder for getting a saved DOB ---------------------------
    private DateTime GetSavedDateOfBirth()
    {
        // Replace with your actual implementation.
        // Return default if no saved DOB found.
        return default;
    }

    // -- Show / Hide Dialogs ---------------------------------------------------
    private void ShowDialog(DialogType dialog)
    {
        YearDialog.SetActive(dialog == DialogType.YearDialog);
        MonthAndDayDialog.SetActive(dialog == DialogType.MonthAndDayDialog);
        DeniedDialog.SetActive(dialog == DialogType.DeniedDialog);
        ConfirmationDialog.SetActive(dialog == DialogType.ConfirmationDialog);
    }

    // Check if input would create a valid year
    // Examples:
    // 1 char:  "1" or "2" is valid (between "1" and current year's first digit)
    // 2 chars: "19" or "20" is valid (between "19" and current year's first 2 digits) 
    // 3 chars: "19X" "20X" is valid (between "190" and current year's first 3 digits)
    private bool IsValidYearInput(string testInput)
    {
        int maxYear = DateTime.Now.Year;
        int minYear = 1900;

        if (!int.TryParse(testInput, out int testYear)) return false;

        // Truncate max/min years to same length as test input
        string maxYearStr = maxYear.ToString().Substring(0, testInput.Length);
        string minYearStr = minYear.ToString().Substring(0, testInput.Length);
        
        int maxTruncated = int.Parse(maxYearStr);
        int minTruncated = int.Parse(minYearStr);

        return testYear >= minTruncated && testYear <= maxTruncated;
    }

    // -- Year Dialog Logic -----------------------------------------------------
    private void OnNumberButtonClicked(string digit)
    {
        if (_yearInput.Length >= 4) return; // Already have 4 digits

        if (!IsValidYearInput(_yearInput + digit)) return;

        // Append if all checks pass
        _yearInput += digit;
        UpdateYearTextField();

        // If we reach 4 digits, evaluate the year
        if (_yearInput.Length == 4)
        {
            EvaluateYearInput();
        }
    }

    private void OnDeleteButtonClicked()
    {
        if (_yearInput.Length > 0)
        {
            _yearInput = _yearInput.Substring(0, _yearInput.Length - 1);
        }
        UpdateYearTextField();
    }

    private void ResetYearInput()
    {
        _yearInput = "";
        UpdateYearTextField();
    }

    private void UpdateYearTextField()
    {
        YearTextField.text = _yearInput;
    }

    private void EvaluateYearInput()
    {
        if (!int.TryParse(_yearInput, out int enteredYear)) return;

        // We'll use December 31 of that year for an "oldest" date
        DateTime dec31 = new DateTime(enteredYear, 12, 31);

        // We'll use January 1 of that year for a "youngest" date
        DateTime jan1 = new DateTime(enteredYear, 1, 1);

        bool dec31Is18OrOlder = IsOlderThan18(dec31);
        bool jan1Is18OrOlder = IsOlderThan18(jan1);

        // 1) If Dec 31 of that year is >= 18 years ago => definitely 18+
        if (dec31Is18OrOlder)
        {
            _dateOfBirth = dec31;
            // Possibly save here
            ShowDialog(DialogType.None);
            _currentState = AgeVerificationState.DateOfBirthGreaterThan18;
            return;
        }

        // 2) If Dec 31 is < 18 but Jan 1 is >= 18 => ambiguous => confirm
        if (!dec31Is18OrOlder && jan1Is18OrOlder)
        {
            _currentState = AgeVerificationState.ConfirmingBirthYear;
            ShowYearConfirmationDialog(_yearInput);
            return;
        }

        // 3) If definitely < 18 or not certain yet => still confirm
        _currentState = AgeVerificationState.ConfirmingBirthYear;
        ShowYearConfirmationDialog(_yearInput);
    }

    private bool IsOlderThan18(DateTime date)
    {
        DateTime today = DateTime.Today;
        DateTime eighteenYearsAgo = today.AddYears(-18);
        return (date <= eighteenYearsAgo);
    }

    // -- Confirmation Dialog Logic (for Year) ----------------------------------
    private void ShowYearConfirmationDialog(string year)
    {
        ShowDialog(DialogType.ConfirmationDialog);
        ConfirmationMessage.text = $"You entered {year} as your birth year. Is this correct?";
    }

    // Called when "Confirm" is pressed on the ConfirmationDialog
    public void OnConfirmDialogConfirm()
    {
        if (_currentState == AgeVerificationState.ConfirmingBirthYear)
        {
            // Ambiguous or under-18 year => show Month/Day for final check
            _currentState = AgeVerificationState.EnteringBirthMonthAndDay;
            ResetMonthDayDialog();
            ShowDialog(DialogType.MonthAndDayDialog);
        }
        else if (_currentState == AgeVerificationState.ConfirmingBirthMonthAndDay)
        {
            // Final confirmation: if user <18 => Deny, else hide
            if (IsOlderThan18(_dateOfBirth))
            {
                _currentState = AgeVerificationState.DateOfBirthGreaterThan18;
                ShowDialog(DialogType.None);
            }
            else
            {
                _currentState = AgeVerificationState.DateOfBirthLessThan18;
                ShowDialog(DialogType.DeniedDialog);
            }
        }
    }

    // Called when "Edit" is pressed on the ConfirmationDialog
    public void OnConfirmDialogEdit()
    {
        if (_currentState == AgeVerificationState.ConfirmingBirthYear)
        {
            // Return to Year entry
            _currentState = AgeVerificationState.EnteringBirthYear;
            ResetYearInput();
            ShowDialog(DialogType.YearDialog);
        }
        else if (_currentState == AgeVerificationState.ConfirmingBirthMonthAndDay)
        {
            // Return to Month/Day entry
            _currentState = AgeVerificationState.EnteringBirthMonthAndDay;
            ShowDialog(DialogType.MonthAndDayDialog);
        }
    }

    // -- Month/Day Dialog Logic -----------------------------------------------
    private void ResetMonthDayDialog()
    {
        MonthDropdown.value = 0; // or whichever default
        DayDropdown.value = 0;
    }

    // Hook this up to the "Submit" button on MonthAndDayDialog
    private void OnMonthDaySubmit()
    {
        int month = MonthDropdown.value; 
        int day = DayDropdown.value;
        Debug.Log($"Month is {month}, dat is {day}");
        
        // Adjust logic if you have placeholders like "-" in index 0
        // For example: month = MonthDropdown.value, day = DayDropdown.value

        if (!int.TryParse(_yearInput, out int enteredYear)) return;
        DateTime finalDate;
        try
        {
            finalDate = new DateTime(enteredYear, month, day);
        }
        catch
        {
            // Invalid date handling
            return;
        }

        _dateOfBirth = finalDate;
        _currentState = AgeVerificationState.ConfirmingBirthMonthAndDay;

        ShowDialog(DialogType.ConfirmationDialog);
        ConfirmationMessage.text = $"You entered {finalDate:MM/dd/yyyy} as your birth date. Is this correct?";
    }
}
