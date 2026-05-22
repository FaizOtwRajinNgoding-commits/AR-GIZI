using System;
using System.Collections.Generic;

[Serializable]
public class Question
{
    public string questionText;
    public string optionA;
    public string optionB;
    public string optionC;
    public string optionD;
    public string correctAnswer; // Isinya: "A", "B", "C", atau "D"
    public string explanation;
}

[Serializable]
public class QuizContainer
{
    public List<Question> questions;
}