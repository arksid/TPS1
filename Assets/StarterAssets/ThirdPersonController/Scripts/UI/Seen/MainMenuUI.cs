// Assets/Scripts/UI/MainMenuUI.cs
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string mainStageSceneName = "MainStage"; // 실제 씬 이름로 변경

    public void OnClick_StartGame()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneAsync(mainStageSceneName);
        else
        {
            Debug.LogError("[MainMenuUI] SceneLoader 인스턴스가 없습니다. 씬에 SceneLoader를 배치하세요.");
        }
    }

    public void OnClick_Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
