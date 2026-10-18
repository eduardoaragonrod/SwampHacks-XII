using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SimpleAnimator : MonoBehaviour
{
	[SerializeField]
	private AnimationClip Animation;

	private PlayableGraph _graph;

	private void Awake()
	{
		if (Animation != null)
		{
			_graph = PlayableGraph.Create("Animation Graph");
			var output = AnimationPlayableOutput.Create(_graph, "Animation Output", gameObject.AddComponent<Animator>());

			var clip = AnimationClipPlayable.Create(_graph, Animation);
			output.SetSourcePlayable(clip);
			_graph.Play();
		}
	}

	private void OnDestroy()
	{
		_graph.Stop();
		_graph.Destroy();
	}
}