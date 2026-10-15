using UnityEngine;
using UnityEngine.UIElements;

public class Titration_DashboardManager : MonoBehaviour
{
    private UIDocument uiDocument;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            // Tell every element in this 3D dashboard to ignore UI clicks so players can still interact with the AR world behind it
            uiDocument.rootVisualElement.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
        }
    }
}