using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AF.UI
{
    public class UILineRenderer : Graphic
    {
        public List<Vector2> points = new List<Vector2>();
        public float thickness = 2f;
        // SASHA: lineColor는 Graphic.color를 사용할 것이므로 별도 필드 불필요.
        // public Color lineColor = Color.white; 
        public Color startColor = Color.white; // SASHA: 시작점 색상 추가
        public Color endColor = Color.white;   // SASHA: 끝점 색상 추가

        // SASHA: RectTransform의 크기에 맞게 포인트를 정규화할지 여부 (0,0에서 1,1 사이로)
        // 기본값은 false로, points에 입력된 좌표를 로컬 UI 좌표로 그대로 사용합니다.
        public bool normalizePoints = false; 

        // SASHA: 선 끝 모양 (Caps) - 매우 기본적인 사각형 끝만 지원
        // public enum LineCap { Butt, Square, Round } // Round는 복잡해서 일단 제외
        // public LineCap startCap = LineCap.Butt;
        // public LineCap endCap = LineCap.Butt;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (points == null || points.Count < 2)
                return;

            Rect rect = GetPixelAdjustedRect(); // UI 요소의 실제 픽셀 크기 가져오기
            Vector2 rectSize = normalizePoints ? rect.size : Vector2.one; // 정규화 시 크기 사용, 아니면 1


            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p0_raw = points[i];
                Vector2 p1_raw = points[i+1];

                // 정규화된 좌표를 로컬 UI 좌표로 변환
                // 피벗(pivot)을 고려하여 좌표를 계산해야 정확합니다.
                // Graphic.rectTransform.pivot은 (0.5, 0.5)가 중앙입니다.
                Vector2 p0 = normalizePoints ? new Vector2(p0_raw.x * rectSize.x - rectTransform.pivot.x * rectSize.x, p0_raw.y * rectSize.y - rectTransform.pivot.y * rectSize.y) : p0_raw;
                Vector2 p1 = normalizePoints ? new Vector2(p1_raw.x * rectSize.x - rectTransform.pivot.x * rectSize.x, p1_raw.y * rectSize.y - rectTransform.pivot.y * rectSize.y) : p1_raw;

                Vector2 direction = (p1 - p0).normalized;
                if (direction == Vector2.zero) continue; // 두 점이 같으면 건너뜀

                Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness / 2f);

                Vector2 v0 = p0 - normal;
                Vector2 v1 = p0 + normal;
                Vector2 v2 = p1 - normal;
                Vector2 v3 = p1 + normal;
                
                // 현재 Graphic 컴포넌트에 설정된 color를 사용합니다.
                // Color32 segmentColor = color; 

                // SASHA: 그라데이션을 위한 색상 계산
                // 전체 선분의 길이에 대한 현재 선분의 시작점과 끝점의 비율을 계산할 수 있지만,
                // 간단하게는 각 선분마다 startColor와 endColor를 그대로 사용하거나, points 리스트의 인덱스에 따라 보간.
                // 여기서는 간단히 각 선분마다 고정된 start/end 색상을 사용하되, Graphic.color.a (알파)를 곱해줌.
                Color32 sColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a * color.a);
                Color32 eColor = new Color(endColor.r, endColor.g, endColor.b, endColor.a * color.a);

                // 버텍스 추가
                int baseIndex = vh.currentVertCount;
                vh.AddVert(v0, sColor, Vector2.zero); // p0 쪽 버텍스들은 startColor 사용 (알파 적용)
                vh.AddVert(v1, sColor, Vector2.up);   
                vh.AddVert(v2, eColor, Vector2.right); // p1 쪽 버텍스들은 endColor 사용 (알파 적용)
                vh.AddVert(v3, eColor, Vector2.one);   

                // 삼각형 추가 (시계 방향 또는 반시계 방향에 따라 순서 중요)
                vh.AddTriangle(baseIndex + 0, baseIndex + 1, baseIndex + 3);
                vh.AddTriangle(baseIndex + 0, baseIndex + 3, baseIndex + 2);
            }
        }

        // Inspector 값 변경 시 업데이트 (더 많은 속성에 대해 추가 가능)
        public void SetPoints(List<Vector2> newPoints)
        {
            if (newPoints == null)
            {
                points.Clear();
            }
            else
            {
                points = new List<Vector2>(newPoints);
            }
            SetVerticesDirty();
        }

        public void SetThickness(float newThickness)
        {
            thickness = Mathf.Max(0, newThickness); // 두께는 0 이상
            SetVerticesDirty();
        }
        
        public void AddPoint(Vector2 point)
        {
            points.Add(point);
            SetVerticesDirty();
        }

        public void ClearPoints()
        {
            points.Clear();
            SetVerticesDirty();
        }

        // SASHA: Graphic.color를 직접 사용하므로 SetColor 메서드는 필요 없음.
        // Graphic.color를 변경하면 자동으로 SetVerticesDirty() 또는 SetMaterialDirty()가 호출됨.
        // public void SetLineColor(Color newLineColor)
        // {
        //     color = newLineColor; // Graphic.color 사용
        //     // SetVerticesDirty(); // Graphic.color 변경 시 자동으로 호출될 수 있음. 확인 필요.
        // }

        // SASHA: 시작/끝 색상 설정 메서드 추가
        public void SetGradientColors(Color newStartColor, Color newEndColor)
        {
            startColor = newStartColor;
            endColor = newEndColor;
            SetVerticesDirty();
        }

        // RectTransform의 크기나 앵커 등이 변경될 때 메시를 다시 그리도록 합니다.
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (normalizePoints) // 정규화된 포인트를 사용할 때만 크기 변경에 반응
            {
                 SetVerticesDirty();
            }
        }

#if UNITY_EDITOR
        // Inspector에서 Graphic.color 필드가 변경될 때 SetVerticesDirty를 호출하여
        // OnPopulateMesh에서 새 색상을 사용하도록 합니다.
        // 기본 Graphic 클래스는 color 변경 시 SetMaterialDirty()만 호출할 수 있어,
        // 버텍스 색상을 직접 사용하는 경우 이것만으로는 OnPopulateMesh가 다시 호출되지 않을 수 있습니다.
        // (Unity 버전에 따라 다를 수 있음)
        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            SetVerticesDirty();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty(); // Inspector 값 변경 시 항상 메시 업데이트
        }
#endif
    }
} 