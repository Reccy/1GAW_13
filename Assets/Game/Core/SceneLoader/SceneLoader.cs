using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    [SerializeField]
    private int m_sceneIndex;

    public void LoadScene()
    {
        SceneManager.LoadScene(m_sceneIndex);
    }
}
