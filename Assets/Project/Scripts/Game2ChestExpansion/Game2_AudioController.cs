public class Game2_AudioController : TrainingAudioController
{
    public void PlayActionResult(Game2_ActionGrade grade)
    {
        switch (grade)
        {
            case Game2_ActionGrade.Excellent:
                PlayExcellent();
                break;
            case Game2_ActionGrade.Good:
                PlayGood();
                break;
            case Game2_ActionGrade.NeedsImprovement:
                PlayNeedsImprovement();
                break;
            default:
                PlayInvalid();
                break;
        }
    }
}
