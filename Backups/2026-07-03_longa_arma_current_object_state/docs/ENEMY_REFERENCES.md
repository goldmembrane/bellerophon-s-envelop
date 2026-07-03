# Enemy Visual References

이 문서는 적대 개체 구현 시 반드시 확인해야 하는 원본 이미지 레퍼런스 목록이다. 원본 파일은 프로젝트 루트의 `image/` 폴더에 있으며, 실제 Unity 런타임 에셋은 구현 단계에서 `Assets/_Project/Art/Enemies/` 또는 해당 시스템 소유 폴더로 가져온다.

## 사용 원칙

- 적대 개체의 실루엣, 비율, 공격 형태, 이동 인상은 `image/`의 대응 이미지를 먼저 확인한 뒤 만든다.
- `image/` 폴더는 원본 레퍼런스 보관 위치로 취급한다. Prefab, Material, Animation, ScriptableObject 같은 Unity 에셋은 `Assets/_Project/` 아래에서 관리한다.
- 구현용 코드 이름은 ASCII 식별자를 우선 사용하고, 표시명에는 한글명을 유지할 수 있다.
- 정면, 측면, 후면, 공격 이미지가 함께 있는 개체는 모델링과 애니메이션 기준으로 함께 사용한다.
- 레퍼런스가 여러 개인 개체는 MVP에서 먼저 하나의 기본형으로 구현하고, 변종은 이후 밸런싱 단계에서 분리한다.

## 레퍼런스 목록

| 개체 | 표시명 | 레퍼런스 파일 |
| --- | --- | --- |
| Parvum | 파르붐 | `image/parvum(파르붐).png`, `image/parvum-back.png`, `image/parvum-beside.png` |
| Fuga | 푸가 | `image/fuga(푸가).png`, `image/fuga-back.png`, `image/fuga-beside.png` |
| Fuga2 | 푸가 변종 | `image/fuga2(푸가).png`, `image/fuga2-back.png`, `image/fuga2-beside.png` |
| Longa Arma | 롱가 아르마 | `image/longa arma(롱가 아르마).png`, `image/longa arma-back.png`, `image/longa arma-beside.png` |
| Tergo | 테르고 | `image/tergo(테르고).png`, `image/tergo-back.png`, `image/tergo-beside.png` |
| Urgere | 우르제레 | `image/urgere(우르제레).png`, `image/urgere-move.png` |
| Societas | 소시에타스 | `image/societas(소시에타스).png`, `image/societas-eating.png` |
| Monstrum | 몬스트룸 | `image/monstrum(몬스트룸).png`, `image/monstrum-back.png`, `image/monstrum-beside.png` |
| Mimesis | 미메시스 | `image/mimesis(미메시스).png`, `image/mimesis-beside.png` |
| Cantabile | 칸타빌레 | `image/cantabile(칸타빌레).png`, `image/cantabile-beside.png` |
| Con Spirito | 콘 스피리토 | `image/con spirito(콘 스피리토).png` |
| Accelerando | 아첼레란도 | `image/accelerando(아첼레란도).png`, `image/accelerando-beside.png` |
| Grave | 그라베 | `image/grave(그라베).png` |
| Smorzando | 스모르찬도 | `image/smorzando(스모르찬도).png`, `image/smorzando-person.png` |
| Ostinato | 오스티나토 | `image/ostinato(오스티나토).png`, `image/ostinato-back.png`, `image/ostinato-beside.png` |
| Dolore | 돌로레 | `image/dolore(돌로레).png`, `image/dolore-attack.png` |
| Negatif | 네거티프 | `image/négatif(네거티프).png` |
| Resistance | 레지스탕스 | `image/résistance(레지스탕스).png` |
| Rebellion | 리벨리온 | `image/rébellion(리벨리온).png` |
| Revolution | 레볼루션 | `image/révolution(레볼루션).png`, `image/révolution-attack.png` |
| Pahhur | 파후르 | `image/pāḫḫur(파후르).png` |
| Kuskursa | 쿠르사 | `image/KUŠkursa(쿠르사).png` |
| Ispant | 이슈판트 | `image/išpant(이슈판트).png`, `image/išpant-armed.png` |
| Atta | 아타 | `image/atta(아타).png` |

## 비적대 참고 이미지

| 대상 | 표시명 | 레퍼런스 파일 |
| --- | --- | --- |
| Transfer | 운송자 | `image/transfer(운송자).png`, `image/transfer-back.png`, `image/transfer-left.png`, `image/transfer-right.png` |

## 구현 우선순위

1. MVP 적대 개체는 수량보다 역할 구분을 우선한다.
2. 첫 구현 대상은 단순 추적형, 경로 차단형, 소리 반응형, 원거리 견제형 중 하나씩 고른다.
3. 각 개체는 먼저 `EnemyDefinition` 데이터, 감지 규칙, 상태 머신, 기본 프리팹으로 만든다.
4. 이미지 기반 외형 반영은 회색 박스 프리팹 다음 단계에서 적용한다.
5. 공격 이미지가 있는 개체는 공격 판정과 애니메이션 타이밍을 함께 검증한다.
