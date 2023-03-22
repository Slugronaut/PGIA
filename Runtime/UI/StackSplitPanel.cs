using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// Visual element for quickly making stack-split panels in UIs.
    /// </summary>
    public class StackSplitPanel : VisualElement
    {
        [Preserve]
        public new class UxmlFactory : UxmlFactory<StackSplitPanel, UxmlTraits> { }
        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits { }


        readonly public TextField Text_SplitAmount;
        public Label Label_Total;



        public StackSplitPanel()
        {
            //add text ui controls for split amount - make it grab control of the mouse
            Text_SplitAmount = new TextField();
            Label_Total = new Label();

            Add(Text_SplitAmount);
            Add(Label_Total);

            Style();
        }

        void Style()
        {
            Text_SplitAmount.style.width = new StyleLength(new Length(50, LengthUnit.Percent));

            Label_Total.style.width = new StyleLength(new Length(50, LengthUnit.Percent));
            Label_Total.style.color = Color.white;
        }

        public void SetQtys(int total)
        {
            Text_SplitAmount.value = Mathf.CeilToInt(total).ToString();
            Label_Total.text = $" / {total}";
        }

    }
}
