#if UNITY_EDITOR
using UnityEditor.Presets;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager를 사용하기 위해 필요
using UnityEngine.UI;

public class Drag : MonoBehaviour
{
    [SerializeField]
    private Transform materialPlace;
    [SerializeField]
    private List<GameObject> completedBreadPrefabs; // 완성된 bread 프리팹 리스트
    [SerializeField]
    private List<GameObject> substituteObject; // 대체할 오브젝트 프리팹

    private Vector2 initialPosition;
    private Vector2 mousePosition;
    private float mouseX, mouseY;
    private int initialLayer;

    public static Dictionary<GameObject, Renderer> renderers = new Dictionary<GameObject, Renderer>();
    public static bool isCompleted = false; // 완성 여부를 체크하는 변수
    private static Dictionary<GameObject, bool> lockedObjects = new Dictionary<GameObject, bool>();
    private static List<GameObject> reachedCopies = new List<GameObject>();

    public static GameObject completedBreadPrefab;

    public static string orderMessage; // 주문 메시지를 저장할 정적 변수

    private void Awake()
    {
        ResetLockedStatus();
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && !renderers.ContainsKey(gameObject))
        {
            renderers.Add(gameObject, renderer);
        }

    }

    private void Start()
    {
        isCompleted = false; // isCompleted 변수 초기화
        initialPosition = transform.position;
        initialLayer = gameObject.layer;
    }

    public static void ResetLockedStatus()
    {
        // 모든 오브젝트의 잠금 상태를 초기화함
        lockedObjects.Clear();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("material"))
        {
            lockedObjects.Add(obj, false);
        }
    }
    private void OnMouseDown()
    {
        // top bread가 목표 위치에 도달했을 경우 다른 오브젝트들이 클릭되지 않도록 함
        if (isCompleted || (CompleteBurger.reachedObjects.Contains("top bread")))
        {
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("material"))
            {
                lockedObjects[obj] = true;
            }
            return;
        }

        if (!lockedObjects[gameObject] && !isCompleted)
        {
            if (!(CompleteBurger.reachedObjects.Contains("top bread")))
            {
                if (gameObject.CompareTag("material"))
                {

                    if (!gameObject.name.Contains("sauce"))
                    {
                        // 오브젝트 복사
                        GameObject duplicate = Instantiate(gameObject, transform.position, transform.rotation);
                        duplicate.name = gameObject.name;

                        Renderer renderer = duplicate.GetComponent<Renderer>();
                        if (renderer != null && !renderers.ContainsKey(duplicate))
                        {
                            renderers.Add(duplicate, renderer);
                        }
                        // 새로운 오브젝트의 sortingOrder 설정
                        int maxSortingOrder = GetMaxSortingOrder();
                        if (renderer != null)
                        {
                            renderer.sortingOrder = maxSortingOrder + 1;
                        }
                    }
                }

            }
        }

    }
    private void OnMouseDrag()
    {
        if (isCompleted) return;
        if (!lockedObjects[gameObject])
        {
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector2(mousePosition.x - mouseX, mousePosition.y - mouseY);

            // 드래그 중에 sortingOrder를 계속 가장 높은 값으로 유지
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                int maxSortingOrder = GetMaxSortingOrder();
                renderer.sortingOrder = maxSortingOrder + 1;
            }
        }
    }
    GameObject sauce;

    private void OnMouseUp()
    {
        if (isCompleted) return;
        if (Mathf.Abs(transform.position.x - materialPlace.position.x) <= 90.0f && Mathf.Abs(transform.position.y - materialPlace.position.y) <= 90.0f)
        {

            if (gameObject.name.Contains("sauce"))
            {
                transform.position = initialPosition;

                // TableCloth 오브젝트의 sortingOrder 값을 가져와서 그 아래로 설정
                GameObject tableCloth = GameObject.Find("Tablecloth");
                if (tableCloth != null)
                {
                    Renderer tableClothRenderer = tableCloth.GetComponent<Renderer>();
                    if (tableClothRenderer != null)
                    {
                        Renderer renderer = gameObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.sortingOrder = tableClothRenderer.sortingOrder - 1;
                        }
                    }
                }
                // 특정 이름을 가진 오브젝트가 목표 위치에 도달하면 대체 오브젝트 생성
                if (substituteObject != null)
                {
                    switch (gameObject.name)
                    {
                        case "teriyaki sauce":
                            sauce = Instantiate(substituteObject[0], new Vector2(materialPlace.position.x - 50.0f, materialPlace.position.y), Quaternion.identity);
                            break;
                        case "tartar sauce":
                            sauce = Instantiate(substituteObject[1], new Vector2(materialPlace.position.x - 50.0f, materialPlace.position.y), Quaternion.identity);
                            break;
                        case "hot sauce":
                            sauce = Instantiate(substituteObject[2], new Vector2(materialPlace.position.x - 50.0f, materialPlace.position.y), Quaternion.identity);
                            break;
                    }

                }
                Renderer renderer_sauce = sauce.GetComponent<Renderer>();
                if (renderer_sauce != null && !renderers.ContainsKey(sauce))
                {
                    renderers.Add(sauce, renderer_sauce);
                }

                // 새로운 오브젝트의 sortingOrder 설정
                int maxSortingOrder = GetMaxSortingOrder();
                if (renderer_sauce != null)
                {
                    renderer_sauce.sortingOrder = maxSortingOrder + 1;
                }

                CompleteBurger.reachedObjects.Add(gameObject.name.Replace("sauce", "").Trim());
                reachedCopies.Add(sauce);

            }
            else
            {
                transform.position = new Vector2(materialPlace.position.x - 50.0f, materialPlace.position.y);
                lockedObjects[gameObject] = true;

                // 잠긴 오브젝트의 sortingOrder 설정
                SetLockedSortingOrder();

                // 오브젝트의 콜라이더 비활성화로 상호작용 차단
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = false;
                }

                // 오브젝트가 목표 위치에 도달했음을 기록
                CompleteBurger.reachedObjects.Add(gameObject.name);
                reachedCopies.Add(gameObject); // 목표 위치에 도달한 오브젝트를 리스트에 추가

                // top_bread 오브젝트가 목표 위치에 도달했을 때 처리
                if (gameObject.name == "top bread")
                {
                    CompleteBread();
                }
            }
        }
        else
        {
            // 목표 위치에 도달하지 못한 오브젝트는 사라짐

            if (gameObject.name.Contains("sauce"))
            {
                // 목표 위치에 도달하지 못한 소스는 원래 위치로 돌아감
                transform.position = initialPosition;

                // TableCloth 오브젝트의 sortingOrder 값을 가져와서 그 아래로 설정
                GameObject tableCloth = GameObject.Find("Tablecloth");
                if (tableCloth != null)
                {
                    Renderer tableClothRenderer = tableCloth.GetComponent<Renderer>();
                    if (tableClothRenderer != null)
                    {
                        Renderer renderer = gameObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.sortingOrder = tableClothRenderer.sortingOrder - 1;
                        }
                    }
                }
            }
            else if (isCompleted || !(CompleteBurger.reachedObjects.Contains("top bread")))
            {
                Destroy(gameObject);
            }
        }
    }

    private int GetMaxSortingOrder()
    {
        int maxSortingOrder = 0;
        foreach (KeyValuePair<GameObject, Renderer> pair in renderers)
        {
            if (pair.Value != null)
            {
                maxSortingOrder = Mathf.Max(maxSortingOrder, pair.Value.sortingOrder);
            }
        }
        return maxSortingOrder;
    }

    private void SetLockedSortingOrder()
    {
        // 잠긴 오브젝트의 sortingOrder 중 가장 높은 값을 찾기
        int maxSortingOrder = GetMaxSortingOrder();

        // 현재 오브젝트의 sortingOrder 설정
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = maxSortingOrder + 1;
        }
    }

    private void CompleteBread()
    {
        isCompleted = true;

        foreach (string reachedObject in CompleteBurger.reachedObjects)
        {
            Debug.Log(reachedObject);
        }

        MadeBurger();

        // 도달한 오브젝트들을 삭제
        CompleteBurger.reachedObjects.Clear();

        // 모든 오브젝트의 드래그 비활성화
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("material"))
        {
            obj.GetComponent<Drag>().enabled = false;
        }

        // 목표 위치에 도달한 복사본 오브젝트 삭제
        foreach (GameObject copy in reachedCopies)
        {
            Destroy(copy);
        }
        reachedCopies.Clear();

    }
    private void MadeBurger()
    {
        transform.position = new Vector2(materialPlace.position.x - 50.0f, materialPlace.position.y);

        // 완성된 bread 생성 (도달한 오브젝트 리스트를 기반으로 결정)
        completedBreadPrefab = CompleteBurger.DetermineBreadType(completedBreadPrefabs);
        if (completedBreadPrefab != null)
        {
            CompleteBurger.completedBread = Instantiate(completedBreadPrefab, transform.position, Quaternion.identity);
        }

    }

    public static void SetOrderMessage(string message)
    {
        orderMessage = message;
    }

    // 레시피 별로 메뉴 체크해서 맞는지 판별
    public static void checkOrder(int persent, string completedBreadPrefab)
    {
        if (string.IsNullOrEmpty(orderMessage))
        {
            Debug.LogError("주문 메시지가 없습니다.");
            return;
        }


        // 잘못된 햄버거
        if (completedBreadPrefab.Equals("WrongBurger_0"))
        {
            persent = 5;
            GameMain.SetPersent(persent);
            return;
        }


        // 레시피에 맞는 햄버거
        if (orderMessage.Equals("오늘은... 불고기 ! 불고기 버거 하나 부탁드려요 !") || orderMessage.Equals("기본 하나요 ! 데리버거인가?"))
        {
            if (completedBreadPrefab.Equals("BulgogiBurger_0")) persent = 10;  // 햄버거 완성
        }
        else if (orderMessage.Equals("띠드버거 주세욤 !!") || orderMessage.Equals("오늘은 느끼한게 땡기네요 치즈 버거 하나요"))
        {
            if (completedBreadPrefab.Equals("CheeseBurger_0")) persent = 10;
        }
        else if (orderMessage.Equals("패티가 따블 !! 더블패티 하나요 !"))
        {
            if (completedBreadPrefab.Equals("DoublePattyBurger_0")) persent = 10;
        }
        else if (orderMessage.Equals("아주 매운 햄버거 주세요 !") || orderMessage.Equals("치킨 버거 주세요 !!"))
        {
            if (completedBreadPrefab.Equals("HotCrispyBurger_0")) persent = 10;
        }
        else if (orderMessage.Equals("새우가 드라마를 찍으면? 대하드라마 !! 하하 !! 새우 버거 하나 주세요 !"))
        {
            if (completedBreadPrefab.Equals("ShrimpBurger_0")) persent = 10;
        }
        GameMain.SetPersent(persent);
    }
}