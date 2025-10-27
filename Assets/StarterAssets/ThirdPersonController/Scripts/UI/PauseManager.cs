using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    [Header("필수 연결")]
    [Tooltip("정지 화면용 Canvas 오브젝트(PauseCanvas)")]
    public GameObject pauseCanvas;

    [Header("선택 연결")]
    [Tooltip("정지 화면 켜질 때 선택될 기본 버튼(없어도 동작함)")]
    public GameObject firstSelected;

    private bool isPaused;

    void Start()
    {
        // 시작 시 꺼두기(실수로 켜져있는 경우 방지)
        SetPause(false);
    }

    void Update()
    {
        // 🔒 증강 선택창이 떠 있으면 ESC 입력 무시
        if (AugmentUIManager.Instance != null && AugmentUIManager.Instance.IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
        {
            TogglePause();
        }
    }
    public void TogglePause()
    {
        SetPause(!isPaused);
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;

        // UI 켜기/끄기
        if (pauseCanvas != null)
            pauseCanvas.SetActive(isPaused);

        // 시간 멈춤/재개
        Time.timeScale = isPaused ? 0f : 1f;

        // 커서 상태
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;

        // UI 선택 포커스(패드/키보드 방향키로 바로 선택 가능)
        if (isPaused && firstSelected != null)
        {
            EventSystem.current?.SetSelectedGameObject(null);
            EventSystem.current?.SetSelectedGameObject(firstSelected);
        }

        // 오디오 일시정지는 필요 시 AudioListener.pause로도 가능
        // AudioListener.pause = isPaused;
    }

    // === UI 버튼에 연결할 메서드들 ===
    public void OnClickResume()
    {
        SetPause(false);
    }

    public void OnClickRestart()
    {
        // 재개 후 씬 다시 로드
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickMainMenu(string mainMenuSceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
