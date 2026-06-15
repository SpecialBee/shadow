using UnityEngine;
using ShadowSeller.Core;

namespace ShadowSeller.UI
{
    // 씬 로드 시 지정한 BGM 트랙을 재생. 각 씬에 하나씩 배치.
    public class SceneBGM : MonoBehaviour
    {
        [SerializeField] private BGMTrack track = BGMTrack.StageAmbient;

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(track);
        }
    }
}
