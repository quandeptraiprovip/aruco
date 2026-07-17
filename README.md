# ArUco Quiz

Trò chơi đố vui cho trẻ dùng **thảm ArUco** trước camera: mỗi câu hỏi hiện 4 đáp án,
trẻ **che thẻ ArUco** (marker 0–3, từ điển `DICT_4X4_50`) của đáp án mình chọn để trả lời.
Unity 2022.3 (Built-in Render Pipeline) + OpenCV for Unity.

Có **2 scene khoá theo môn**, mỗi scene một thiết kế màn Chuẩn bị riêng:

| Môn | Scene |
|-----|-------|
| Toán | `Assets/Scenes/ArucoQuizMath.unity` |
| Tiếng Anh | `Assets/Scenes/ArucoQuizEnglish.unity` |

---

## Yêu cầu

- **Unity `2022.3.62f3`** (đúng phiên bản này — cài qua Unity Hub). Built-in RP.
- **OpenCV for Unity** của **Enox Software** (asset **trả phí** trên Asset Store).
  → **Không nằm trong repo** (bị `.gitignore`), phải tự import khi clone (xem bên dưới).
  Project được build với native lib **OpenCV 4.5** (dòng OpenCVForUnity `2.5.x`).
- **Webcam** + (trên macOS) quyền truy cập camera.
- *(Tuỳ chọn)* Python 3 — chỉ để tạo lại file audio; **không cần** để chạy game.

---

## Setup khi clone về máy mới

```bash
git clone <repo-url>
cd arucoaruco
```

1. **Import OpenCV for Unity** (bước bắt buộc — thiếu nó project **không compile được**
   vì mọi script nhận diện dựa trên namespace `OpenCVForUnity`):
   - Mở Unity Hub → cài **Unity 2022.3.62f3**.
   - Mở project bằng đúng bản đó. Lần đầu sẽ có lỗi biên dịch (chưa có OpenCV) — bình thường.
   - Trong Editor: **Window → Package Manager → My Assets** → tải & **Import** *OpenCV for Unity*
     (hoặc double-click file `.unitypackage` đã mua). Import vào đúng thư mục
     `Assets/OpenCVForUnity/`.
   - Chờ Unity biên dịch lại; lỗi sẽ hết.
2. Unity tự dựng lại thư mục `Library/` (không có trong repo) — lần mở đầu hơi lâu.
3. Nếu có popup **TMP Essentials**, cứ Import (assets TMP đã kèm sẵn trong repo nên thường không hỏi).

> Repo **không** chứa: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, file IDE (`*.csproj`, `*.sln`),
> và `Assets/OpenCVForUnity/`. Tất cả đều được sinh lại / import lại như trên.

---

## Chạy game

1. Mở scene muốn chơi (`ArucoQuizMath.unity` hoặc `ArucoQuizEnglish.unity`).
2. Bấm **Play**. Lần đầu macOS sẽ hỏi **quyền camera** — phải **Cho phép** thì feed webcam mới hiện.
3. Đưa **thảm ArUco** (đủ 4 marker 0–3) vào khung camera. Bộ nhận diện chỉ "lên nòng"
   sau khi thấy đủ cả 4 marker; **che** thẻ của đáp án đã chọn và **giữ ~2 giây** để chốt.

### In thảm ArUco
- Marker dùng `DICT_4X4_50`, ID **0, 1, 2, 3** (tương ứng đáp án A, B, C, D).
- Ảnh marker: `Assets/Textures/ArucoMarkers/`. Nếu thiếu, Editor tự sinh khi build scene,
  hoặc dùng menu **Aruco Quiz** để tạo lại.

### Ngân hàng câu hỏi
- `Assets/StreamingAssets/Math.json` (Toán) và `Assets/StreamingAssets/ESL.json` (Tiếng Anh).
  Sửa trực tiếp để đổi/thêm câu hỏi.

---

## Dựng lại scene từ script (menu "Aruco Quiz")

Toàn bộ layout 2 scene được **sinh bằng editor script** (`Assets/Editor/ArucoQuizSceneBuilder.cs`).
Sau khi sửa builder, dựng lại qua menu trên thanh Editor:

- **Aruco Quiz → Build Math Scene** → ghi đè `ArucoQuizMath.unity`
- **Aruco Quiz → Build English Scene** → ghi đè `ArucoQuizEnglish.unity`
- **Aruco Quiz → Capture Math/English Screenshots** → chụp ảnh preview scene

Hoặc chạy headless (khi **không** có Editor nào đang mở project — nếu không sẽ vướng lock):

```bash
UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod ArucoQuizSceneBuilder.BuildMathFromCommandLine
"$UNITY" -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod ArucoQuizSceneBuilder.BuildEnglishFromCommandLine
```

---

## Tạo lại audio (tuỳ chọn)

File nhạc/SFX đã có sẵn trong `Assets/Audio/BuiltIn/`. Muốn tạo lại (chỉ dùng thư viện chuẩn Python):

```bash
python3 Tools/generate_quiz_audio.py
```

---

## Cấu hình Git (khuyến nghị 1 lần / máy)

`.gitattributes` khai báo **Smart Merge** cho file YAML của Unity (scene/prefab/asset). Bật bằng:

```bash
git config merge.unityyamlmerge.driver \
  '"/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/Tools/UnityYAMLMerge" merge -p %O %B %A %A'
```
