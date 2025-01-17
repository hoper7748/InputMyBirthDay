using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class BirthDayInputManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField birthDayInputField;
    [SerializeField] private TextMeshProUGUI BirthYearInputField;

    [Header("Month"), Space(10)]
    [SerializeField] private TextMeshProUGUI ErrorMessage;
    [SerializeField] private TextMeshProUGUI YearDropDown;
    [SerializeField] private TMP_Dropdown MonthDropDown;
    [SerializeField] private TMP_Dropdown DayDropDown;
    [SerializeField] private Button SubmitButton;
    // Start is called before the first frame update
    void Start()
    {
        //YearDropDown = 
        DayDropDown.options.Clear();
        ErrorMessage.gameObject.SetActive(false);
        DayDropDown.options.Add(new TMP_Dropdown.OptionData("-"));
        SubmitButton.onClick.AddListener(delegate {
            YearCalculation();
        });
        MonthDropDown.onValueChanged.AddListener(delegate
        {
            ChangedMonth();
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void YearCalculation()
    {
        string input = birthDayInputField.text + "-" + MonthDropDown.value + "-" + DayDropDown.value;
        if(DateTime.TryParse(input, out DateTime birthDate))
        {
            DateTime today = DateTime.Now.Date;
            int age = today.Year - birthDate.Year;
            if(birthDate > today.AddYears(-age))
            {
                age--;
            }
            GetResult(age);
        }
    }

    private void GetResult(int age)
    {
        if(age <= 18)
        {
            ErrorMessage.gameObject.SetActive(true);
            ErrorMessage.text = "You must be 18 years or older to play this game.";
        }
        else
        {
            ErrorMessage.gameObject .SetActive(true);
            ErrorMessage.text = "Please enter your month and date of birth as well";
        }
    }

    private void ShowErrorMessage()
    {
        ErrorMessage.gameObject.SetActive(true);
    }

    private void ChangedMonth()
    {
        DayDropDown.options.Clear();
        switch (MonthDropDown.value)
        {
            case 0:
                break;
            case 1:
            case 3:
            case 5:
            case 7:
            case 8:
            case 10:
            case 12:
                DayOptionSet(31);
                break;
            case 4:
            case 6:
            case 9:
            case 11:
                DayOptionSet(30);
                break;

            case 2:
                // 4년 계산하여 윤달인지 체크
                DayOptionSet(int.Parse(birthDayInputField.text) % 4 == 0 ? 29:28);
                break;

            default:
                break;
        }
    }

    private void DayOptionSet(int maxValue)
    {
        for (int i = 0; i <= maxValue; ++i)
        {
            TMP_Dropdown.OptionData data = new TMP_Dropdown.OptionData();
            data.text = i.ToString();
            DayDropDown.options.Add(data);
        }
    }

}
