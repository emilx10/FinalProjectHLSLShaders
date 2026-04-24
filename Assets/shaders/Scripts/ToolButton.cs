using UnityEngine;

public class ToolButton : MonoBehaviour
{
    public PaintController paintController;

    public PaintController.ToolMode toolToSet;

    public void OnButtonPressed()
    {
        if (paintController == null)
            return;

        switch (toolToSet)
        {
            case PaintController.ToolMode.Grow:
                paintController.SetToolGrow();
                break;

            case PaintController.ToolMode.Cut:
                paintController.SetToolCut();
                break;

            case PaintController.ToolMode.Color:
                paintController.SetToolColor();
                break;
        }
    }
}