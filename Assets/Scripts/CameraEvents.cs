using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectSpace
{
    public class CameraEvents : MonoBehaviour
    {

        private void OnPostRender()
        {
            ObjectSpaceController.instance.OnPostRender();
        }

    }
}