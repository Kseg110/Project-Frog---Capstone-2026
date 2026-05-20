using UnityEngine;

[RequireComponent(typeof(PlayerAnchor))]
public class PlayerAnchorVFX : MonoBehaviour
{
    [SerializeField] private PlayerAnchor _playerAnchor;

    private AnchorVFX _currentAnchorVFX;
    private bool _wasTethered;

    private void Update()
    {
        if (_playerAnchor == null) return;

        bool isTethered = _playerAnchor.IsTethered;

        if (isTethered && !_wasTethered)
            OnAttach();
        else if (!isTethered && _wasTethered)
            OnDetach();

        _wasTethered = isTethered;
    }

    private void OnAttach()
    {
        if (_playerAnchor.CurrentAnchor == null) return;

        Debug.Log("OnAttach fired on: " + _playerAnchor.CurrentAnchor.name);
        _currentAnchorVFX = _playerAnchor.CurrentAnchor.GetComponent<AnchorVFX>();
        Debug.Log("AnchorVFX found: " + (_currentAnchorVFX != null));
        _currentAnchorVFX?.PlayActivation();
    }

    private void OnDetach()
    {
        _currentAnchorVFX?.StopActivation();
        _currentAnchorVFX = null;
    }

}