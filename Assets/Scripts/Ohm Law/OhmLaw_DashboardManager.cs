using UnityEngine;
using UnityEngine.UIElements;

public class OhmLaw_DashboardManager : MonoBehaviour
{
    private UIDocument uiDocument;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            // Lock the dashboard so it cannot be interacted with or moved
            uiDocument.rootVisualElement.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
        }
    }
}