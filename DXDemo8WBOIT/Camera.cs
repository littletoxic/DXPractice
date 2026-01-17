using System.Drawing;
using System.Numerics;

namespace DXDemo8WBOIT;

internal sealed class Camera {
    private Vector3 _eyePosition;    // 摄像机在世界空间下的位置
    private Vector3 _focusPosition;  // 摄像机在世界空间下观察的焦点位置
    private Vector3 _upDirection;    // 世界空间垂直向上的向量

    // 摄像机观察方向的单位向量，用于前后移动
    private Vector3 _viewDirection;

    // 焦距，摄像机原点与焦点的距离
    private readonly float _focalLength;

    // 摄像机向右方向的单位向量，用于左右移动
    private Vector3 _rightDirection;

    private Point _lastCursorPoint;         // 上一次鼠标的位置

    private const float FovAngleY = MathF.PI / 4.0f;   // 垂直视场角
    private const float AspectRatio = 4f / 3f;   // 投影窗口宽高比
    private const float NearZ = 0.1f;            // 近平面到原点的距离
    private const float FarZ = 1000f;            // 远平面到原点的距离

    // 模型矩阵，这里我们让模型旋转 30° 就行，注意这里只是一个示例，后文我们会将它移除，每个模型都应该拥有相对独立的模型矩阵
    private Matrix4x4 _modelMatrix;
    // 观察矩阵，注意前两个参数是点，第三个参数才是向量
    private Matrix4x4 _viewMatrix;
    // 投影矩阵(注意近平面和远平面距离不能 <= 0!)
    private Matrix4x4 _projectionMatrix;

    internal Matrix4x4 MVPMatrix {
        get {
            _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection);
            return _viewMatrix * _projectionMatrix; // MVP 矩阵
        }
    }

    internal Matrix4x4 ViewMatrix {
        get {
            return Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection);
        }
    }

    internal Camera() {
        _modelMatrix = Matrix4x4.CreateRotationY(30.0f);  // 模型矩阵，模型空间 -> 世界空间
        _viewMatrix = Matrix4x4.CreateLookAtLeftHanded(_eyePosition, _focusPosition, _upDirection); // 观察矩阵，世界空间 -> 观察空间
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(FovAngleY, AspectRatio, NearZ, FarZ); // 投影矩阵，观察空间 -> 齐次裁剪空间

        _eyePosition = new Vector3(4, 5, -4);
        _focusPosition = new Vector3(4, 3, 4);
        _upDirection = new Vector3(0, 1, 0);

        _viewDirection = Vector3.Normalize(_focusPosition - _eyePosition);
        _focalLength = Vector3.Distance(_focusPosition, _eyePosition);
        _rightDirection = Vector3.Normalize(Vector3.Cross(_viewDirection, _upDirection));
    }

    // 摄像机前后移动，参数 Stride 是移动速度 (步长)，正数向前移动，负数向后移动
    internal void Walk(float stride) {
        _eyePosition += stride * _viewDirection;
        _focusPosition += stride * _viewDirection;
    }

    // 摄像机左右移动，参数 Stride 是移动速度 (步长)，正数向左移动，负数向右移动
    internal void Strafe(float stride) {
        _eyePosition += stride * _rightDirection;
        _focusPosition += stride * _rightDirection;
    }

    // 鼠标在屏幕空间 y 轴上移动，相当于摄像机以向右的向量 RightDirection 向上向下旋转，人眼往上下看
    private void RotateByY(float angleY) {
        var r = Matrix4x4.CreateFromAxisAngle(_rightDirection, -angleY);

        _upDirection = Vector3.TransformNormal(_upDirection, r);
        _viewDirection = Vector3.TransformNormal(_viewDirection, r);

        _focusPosition = _eyePosition + _focalLength * _viewDirection;
    }

    // 鼠标在屏幕空间 x 轴上移动，相当于摄像机绕世界空间的 y 轴向左向右旋转，人眼往左右看
    private void RotateByX(float angleX) {
        var r = Matrix4x4.CreateRotationY(angleX);

        _upDirection = Vector3.TransformNormal(_upDirection, r);
        _viewDirection = Vector3.TransformNormal(_viewDirection, r);
        _rightDirection = Vector3.TransformNormal(_rightDirection, r);

        _focusPosition = _eyePosition + _focalLength * _viewDirection;
    }

    internal void UpdateLastCursorPos() {
        GetCursorPos(out _lastCursorPoint);
    }

    // 当鼠标左键长按并移动时，旋转摄像机视角
    internal void CameraRotate() {
        GetCursorPos(out var currentCursorPoint);

        float deltaX = currentCursorPoint.X - _lastCursorPoint.X;
        float deltaY = currentCursorPoint.Y - _lastCursorPoint.Y;

        float angleX = deltaX * (MathF.PI / 180.0f) * 0.25f;
        float angleY = deltaY * (MathF.PI / 180.0f) * 0.25f;

        RotateByY(angleY);
        RotateByX(angleX);

        UpdateLastCursorPos();
    }
}
