// 1. 플레이어 및 키보드 종류별 계층형 입력 테스트 데이터 (Mock Dataset)
const mockDataset = {
  "Player 1": {
    code: "Player 1",
    name: "테스터 Alpha",
    keyboards: {
      "KeyboardType A": {
        typeCode: "KeyboardType A",
        title: "기본형 멤브레인 키보드",
        keyCounts: {
          "Space": 88,
          "W": 64,
          "D": 51,
          "A": 46,
          "S": 39,
          "F": 32,
          "E": 28,
          "ShiftLeft": 25,
          "R": 21,
          "Enter": 17,
          "Tab": 14,
          "Q": 12,
          "Digit1": 8,
          "Digit2": 7,
          "Escape": 5,
          "C": 4,
          "V": 4,
          "ArrowUp": 3,
          "ArrowDown": 2,
          "ControlLeft": 2,
          "Backspace": 2,
          "B": 1,
          "G": 1
        }
      },
      "KeyboardType B": {
        typeCode: "KeyboardType B",
        title: "게이밍 기계식 키보드",
        keyCounts: {
          "Space": 165,
          "Q": 102,
          "E": 94,
          "R": 88,
          "W": 72,
          "ShiftLeft": 60,
          "F": 50,
          "Digit1": 36,
          "Digit2": 31,
          "Digit3": 25,
          "Tab": 21,
          "A": 18,
          "D": 16,
          "Escape": 10,
          "C": 9,
          "ControlLeft": 7
        }
      },
      "KeyboardType C": {
        typeCode: "KeyboardType C",
        title: "아케이드 / 방향키 조작 키보드",
        keyCounts: {
          "ArrowUp": 88,
          "ArrowDown": 80,
          "ArrowRight": 75,
          "ArrowLeft": 70,
          "Space": 62,
          "Enter": 56,
          "Z": 46,
          "X": 42,
          "C": 34,
          "Backspace": 25,
          "ShiftRight": 19,
          "Escape": 12,
          "Digit1": 6
        }
      }
    }
  },
  "Player 2": {
    code: "Player 2",
    name: "테스터 Bravo",
    keyboards: {
      "KeyboardType A": {
        typeCode: "KeyboardType A",
        title: "기본형 멤브레인 키보드",
        keyCounts: {
          "W": 92,
          "A": 84,
          "D": 78,
          "S": 70,
          "Space": 42,
          "F": 25,
          "E": 20,
          "ShiftLeft": 18,
          "Tab": 12,
          "Escape": 7,
          "Enter": 6,
          "Digit1": 5
        }
      },
      "KeyboardType B": {
        typeCode: "KeyboardType B",
        title: "게이밍 기계식 키보드",
        keyCounts: {
          "Q": 148,
          "W": 132,
          "E": 118,
          "R": 112,
          "Space": 98,
          "ShiftLeft": 82,
          "F": 64,
          "Digit1": 48,
          "Digit2": 42,
          "Digit3": 38,
          "Tab": 28,
          "Escape": 14,
          "ControlLeft": 10
        }
      },
      "KeyboardType C": {
        typeCode: "KeyboardType C",
        title: "아케이드 / 방향키 조작 키보드",
        keyCounts: {
          "Z": 105,
          "X": 98,
          "C": 92,
          "Space": 75,
          "ArrowUp": 54,
          "ArrowDown": 48,
          "ArrowLeft": 44,
          "ArrowRight": 42,
          "Enter": 32,
          "Backspace": 16,
          "Escape": 8
        }
      }
    }
  },
  "Player 3": {
    code: "Player 3",
    name: "테스터 Charlie",
    keyboards: {
      "KeyboardType A": {
        typeCode: "KeyboardType A",
        title: "기본형 멤브레인 키보드",
        keyCounts: {
          "Space": 32,
          "Enter": 30,
          "Backspace": 22,
          "W": 20,
          "S": 16,
          "A": 14,
          "D": 14,
          "F": 9,
          "Escape": 8,
          "Tab": 6,
          "Digit1": 4
        }
      },
      "KeyboardType B": {
        typeCode: "KeyboardType B",
        title: "게이밍 기계식 키보드",
        keyCounts: {
          "Space": 55,
          "W": 45,
          "E": 38,
          "Q": 32,
          "R": 28,
          "ShiftLeft": 22,
          "F": 19,
          "Tab": 14,
          "Digit1": 11,
          "Escape": 8,
          "C": 5
        }
      },
      "KeyboardType C": {
        typeCode: "KeyboardType C",
        title: "아케이드 / 방향키 조작 키보드",
        keyCounts: {
          "ArrowDown": 118,
          "ArrowUp": 112,
          "ArrowLeft": 102,
          "ArrowRight": 96,
          "Space": 44,
          "Enter": 38,
          "Z": 28,
          "X": 22,
          "Backspace": 19,
          "Escape": 6,
          "ShiftRight": 5
        }
      }
    }
  }
};

// 2. 텐키리스(TKL, 87키) 키보드 레이아웃 정의
const mainLayout = [
  // F-열 (Row 0)
  [
    { code: "Escape", label: "ESC" },
    { spacer: "f-gap-large" },
    { code: "F1", label: "F1" },
    { code: "F2", label: "F2" },
    { code: "F3", label: "F3" },
    { code: "F4", label: "F4" },
    { spacer: "f-gap" },
    { code: "F5", label: "F5" },
    { code: "F6", label: "F6" },
    { code: "F7", label: "F7" },
    { code: "F8", label: "F8" },
    { spacer: "f-gap" },
    { code: "F9", label: "F9" },
    { code: "F10", label: "F10" },
    { code: "F11", label: "F11" },
    { code: "F12", label: "F12" },
  ],
  // 숫자열 (Row 1)
  [
    { code: "Backquote", label: "~" },
    { code: "Digit1", label: "1" },
    { code: "Digit2", label: "2" },
    { code: "Digit3", label: "3" },
    { code: "Digit4", label: "4" },
    { code: "Digit5", label: "5" },
    { code: "Digit6", label: "6" },
    { code: "Digit7", label: "7" },
    { code: "Digit8", label: "8" },
    { code: "Digit9", label: "9" },
    { code: "Digit0", label: "0" },
    { code: "Minus", label: "-" },
    { code: "Equal", label: "=" },
    { code: "Backspace", label: "Backspace", widthClass: "w-2" },
  ],
  // QWERTY열 (Row 2)
  [
    { code: "Tab", label: "Tab", widthClass: "w-1-5" },
    { code: "Q", label: "Q" },
    { code: "W", label: "W" },
    { code: "E", label: "E" },
    { code: "R", label: "R" },
    { code: "T", label: "T" },
    { code: "Y", label: "Y" },
    { code: "U", label: "U" },
    { code: "I", label: "I" },
    { code: "O", label: "O" },
    { code: "P", label: "P" },
    { code: "BracketLeft", label: "[" },
    { code: "BracketRight", label: "]" },
    { code: "Backslash", label: "\\", widthClass: "w-1-5" },
  ],
  // 홈열 (Row 3)
  [
    { code: "CapsLock", label: "Caps", widthClass: "w-1-75" },
    { code: "A", label: "A" },
    { code: "S", label: "S" },
    { code: "D", label: "D" },
    { code: "F", label: "F" },
    { code: "G", label: "G" },
    { code: "H", label: "H" },
    { code: "J", label: "J" },
    { code: "K", label: "K" },
    { code: "L", label: "L" },
    { code: "Semicolon", label: ";" },
    { code: "Quote", label: "'" },
    { code: "Enter", label: "Enter", widthClass: "w-2-25" },
  ],
  // 하단열 (Row 4)
  [
    { code: "ShiftLeft", label: "Shift", widthClass: "w-2-25" },
    { code: "Z", label: "Z" },
    { code: "X", label: "X" },
    { code: "C", label: "C" },
    { code: "V", label: "V" },
    { code: "B", label: "B" },
    { code: "N", label: "N" },
    { code: "M", label: "M" },
    { code: "Comma", label: "<" },
    { code: "Period", label: ">" },
    { code: "Slash", label: "?" },
    { code: "ShiftRight", label: "Shift", widthClass: "w-2-75" },
  ],
  // 컨트롤/스페이스바열 (Row 5)
  [
    { code: "ControlLeft", label: "Ctrl", widthClass: "w-1-25" },
    { code: "MetaLeft", label: "Win", widthClass: "w-1-25" },
    { code: "AltLeft", label: "Alt", widthClass: "w-1-25" },
    { code: "Space", label: "Space", widthClass: "w-6-25" },
    { code: "AltRight", label: "Alt", widthClass: "w-1-25" },
    { code: "MetaRight", label: "Win", widthClass: "w-1-25" },
    { code: "ContextMenu", label: "Menu", widthClass: "w-1-25" },
    { code: "ControlRight", label: "Ctrl", widthClass: "w-1-25" },
  ]
];

const navLayout = [
  // F-열 상단 기능
  [
    { code: "PrintScreen", label: "PrtSc" },
    { code: "ScrollLock", label: "ScrLk" },
    { code: "Pause", label: "Pause" },
  ],
  // 6키 클러스터 상단
  [
    { code: "Insert", label: "Ins" },
    { code: "Home", label: "Home" },
    { code: "PageUp", label: "PgUp" },
  ],
  // 6키 클러스터 하단
  [
    { code: "Delete", label: "Del" },
    { code: "End", label: "End" },
    { code: "PageDown", label: "PgDn" },
  ],
  // 빈 여백 (Row 3)
  null,
  // 방향키 상단 (Row 4)
  [
    { empty: true },
    { code: "ArrowUp", label: "▲" },
    { empty: true },
  ],
  // 방향키 하단 (Row 5)
  [
    { code: "ArrowLeft", label: "◀" },
    { code: "ArrowDown", label: "▼" },
    { code: "ArrowRight", label: "▶" },
  ]
];

// 3. 히트맵 색상 계산 함수
function getHeatmapColor(count, maxCount) {
  if (!count || count === 0) {
    return {
      bg: "var(--key-base)",
      border: "var(--key-border)",
      color: "var(--key-text)",
      boxShadow: "0 3px 0 rgba(0,0,0,0.4)"
    };
  }

  const ratio = Math.min(count / maxCount, 1.0);

  let r, g, b;
  if (ratio < 0.25) {
    const t = ratio / 0.25;
    r = Math.round(2 + t * 14);
    g = Math.round(132 + t * 50);
    b = Math.round(199 + t * -30);
  } else if (ratio < 0.5) {
    const t = (ratio - 0.25) / 0.25;
    r = Math.round(16 + t * 229);
    g = Math.round(185 + t * -27);
    b = Math.round(129 + t * -118);
  } else if (ratio < 0.75) {
    const t = (ratio - 0.5) / 0.25;
    r = Math.round(245 + t * -6);
    g = Math.round(158 + t * -90);
    b = Math.round(11 + t * 57);
  } else {
    const t = (ratio - 0.75) / 0.25;
    r = Math.round(239 + t * 16);
    g = Math.round(68 + t * -40);
    b = Math.round(68 + t * -30);
  }

  const glowOpacity = Math.max(0.2, ratio * 0.75);
  const shadow = `0 3px 0 rgba(0,0,0,0.4), 0 0 ${Math.round(ratio * 16)}px rgba(${r}, ${g}, ${b}, ${glowOpacity})`;

  return {
    bg: `rgba(${r}, ${g}, ${b}, ${0.45 + ratio * 0.5})`,
    border: `rgb(${r}, ${g}, ${b})`,
    color: "#ffffff",
    boxShadow: shadow
  };
}

// 4. 상태 관리
const playerKeys = Object.keys(mockDataset);
let currentPlayerIndex = 0;
let currentTypeIndex = 0;

// 5. 키보드 히트맵 렌더링 (View 2)
function renderKeyboard(keyCounts) {
  const counts = Object.values(keyCounts);
  const maxCount = Math.max(...counts, 1);

  document.getElementById("maxLegendText").innerText = `최다 (${maxCount}회)`;

  const mainSection = document.getElementById("mainSection");
  const navSection = document.getElementById("navSection");
  mainSection.innerHTML = "";
  navSection.innerHTML = "";

  // 메인 타이핑 구역 렌더링
  mainLayout.forEach((row, rowIndex) => {
    const rowEl = document.createElement("div");
    rowEl.className = "kb-row" + (rowIndex === 0 ? " f-row" : "");

    row.forEach(item => {
      if (item.spacer) {
        const spacer = document.createElement("div");
        spacer.className = item.spacer;
        rowEl.appendChild(spacer);
        return;
      }

      const count = keyCounts[item.code] || 0;
      const style = getHeatmapColor(count, maxCount);

      const keyEl = document.createElement("div");
      keyEl.className = `key ${item.widthClass || ""}${count > 0 ? " has-count" : ""}`;
      keyEl.style.backgroundColor = style.bg;
      keyEl.style.borderColor = style.border;
      keyEl.style.color = style.color;
      keyEl.style.boxShadow = style.boxShadow;
      keyEl.title = `${item.label} (${item.code}): ${count}회`;

      keyEl.innerHTML = `
        <span class="key-label">${item.label}</span>
        <span class="key-count">${count > 0 ? count : ""}</span>
      `;

      rowEl.appendChild(keyEl);
    });

    mainSection.appendChild(rowEl);
  });

  // 네비게이션 구역 렌더링
  navLayout.forEach((row, rowIndex) => {
    if (!row) {
      const spacer = document.createElement("div");
      spacer.className = "nav-spacer";
      navSection.appendChild(spacer);
      return;
    }

    const rowEl = document.createElement("div");
    rowEl.className = "kb-row" + (rowIndex === 0 ? " f-row" : "");

    row.forEach(item => {
      if (item.empty) {
        const emptyEl = document.createElement("div");
        emptyEl.style.width = "var(--key-unit)";
        emptyEl.style.height = "var(--key-height)";
        rowEl.appendChild(emptyEl);
        return;
      }

      const count = keyCounts[item.code] || 0;
      const style = getHeatmapColor(count, maxCount);

      const keyEl = document.createElement("div");
      keyEl.className = `key${count > 0 ? " has-count" : ""}`;
      keyEl.style.backgroundColor = style.bg;
      keyEl.style.borderColor = style.border;
      keyEl.style.color = style.color;
      keyEl.style.boxShadow = style.boxShadow;
      keyEl.title = `${item.label} (${item.code}): ${count}회`;

      keyEl.innerHTML = `
        <span class="key-label">${item.label}</span>
        <span class="key-count">${count > 0 ? count : ""}</span>
      `;

      rowEl.appendChild(keyEl);
    });

    navSection.appendChild(rowEl);
  });
}

// 6. 상위 키 순위 차트 렌더링 (View 3)
function renderRanking(keyCounts) {
  const rankingList = document.getElementById("rankingList");
  rankingList.innerHTML = "";

  const sortedKeys = Object.entries(keyCounts)
    .filter(([_, count]) => count > 0)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 10);

  const maxCount = sortedKeys[0] ? sortedKeys[0][1] : 1;
  const totalCount = Object.values(keyCounts).reduce((acc, cur) => acc + cur, 0);

  sortedKeys.forEach(([code, count], index) => {
    const percent = ((count / totalCount) * 100).toFixed(1);
    const barWidth = ((count / maxCount) * 100).toFixed(1);
    const rankClass = index < 3 ? `rank-${index + 1}` : "";

    const itemEl = document.createElement("div");
    itemEl.className = `ranking-item ${rankClass}`;
    itemEl.innerHTML = `
      <div class="ranking-header">
        <div class="ranking-key">
          <span class="rank-num">${index + 1}</span>
          <span>${code}</span>
        </div>
        <div class="ranking-stat">
          <strong>${count}회</strong> (${percent}%)
        </div>
      </div>
      <div class="bar-track">
        <div class="bar-fill" style="width: ${barWidth}%"></div>
      </div>
    `;

    rankingList.appendChild(itemEl);
  });
}

// 7. 탭 목록 업데이트
function updateTabs() {
  // 플레이어 탭 업데이트
  const playerTabs = document.getElementById("playerTabs").querySelectorAll(".player-tab-btn");
  playerTabs.forEach((btn, idx) => {
    if (idx === currentPlayerIndex) {
      btn.classList.add("active");
    } else {
      btn.classList.remove("active");
    }
  });

  // 키보드 타입 탭 업데이트
  const typeTabs = document.getElementById("typeTabs").querySelectorAll(".type-tab-btn");
  typeTabs.forEach((btn, idx) => {
    if (idx === currentTypeIndex) {
      btn.classList.add("active");
    } else {
      btn.classList.remove("active");
    }
  });
}

// 8. 현재 선택된 플레이어 & 키보드 타입으로 뷰 전체 갱신
function updateView() {
  const currentPlayerKey = playerKeys[currentPlayerIndex];
  const playerData = mockDataset[currentPlayerKey];

  const keyboardTypeKeys = Object.keys(playerData.keyboards);
  const currentTypeKey = keyboardTypeKeys[currentTypeIndex];
  const keyboardData = playerData.keyboards[currentTypeKey];

  // 전체 데이터셋 총합 계산
  let grandTotal = 0;
  Object.values(mockDataset).forEach(p => {
    Object.values(p.keyboards).forEach(kb => {
      grandTotal += Object.values(kb.keyCounts).reduce((a, b) => a + b, 0);
    });
  });
  document.getElementById("summaryTotal").innerText = `전체 데이터 총합: ${grandTotal.toLocaleString()}회`;

  // 1) 플레이어 정보 표시
  document.getElementById("currentPlayerCode").innerText = playerData.code;
  document.getElementById("currentPlayerName").innerText = playerData.name;

  // 2) 키보드 타입 정보 표시
  document.getElementById("currentTypeCode").innerText = keyboardData.typeCode;
  document.getElementById("currentTypeName").innerText = keyboardData.title;

  // 3) 현재 선택된 세트 타수 계산 및 배지 갱신
  const currentCounts = Object.values(keyboardData.keyCounts);
  const currentTotal = currentCounts.reduce((a, b) => a + b, 0);
  document.getElementById("typeTotalBadge").innerText = `${playerData.code} · ${keyboardData.typeCode} 입력: ${currentTotal.toLocaleString()}회`;

  // 탭 상태 동기화
  updateTabs();

  // 뷰 리렌더링
  renderKeyboard(keyboardData.keyCounts);
  renderRanking(keyboardData.keyCounts);
}

// 9. 초기화 및 이벤트 리스너 등록
document.addEventListener("DOMContentLoaded", () => {
  // 플레이어 탭 생성
  const playerTabsContainer = document.getElementById("playerTabs");
  playerTabsContainer.innerHTML = "";
  playerKeys.forEach((key, idx) => {
    const btn = document.createElement("button");
    btn.className = "player-tab-btn" + (idx === currentPlayerIndex ? " active" : "");
    btn.innerText = key;
    btn.addEventListener("click", () => {
      currentPlayerIndex = idx;
      updateView();
    });
    playerTabsContainer.appendChild(btn);
  });

  // 키보드 타입 탭 생성 (첫 플레이어의 키보드 목록 기준)
  const keyboardTypeKeys = Object.keys(mockDataset[playerKeys[0]].keyboards);
  const typeTabsContainer = document.getElementById("typeTabs");
  typeTabsContainer.innerHTML = "";
  keyboardTypeKeys.forEach((key, idx) => {
    const btn = document.createElement("button");
    btn.className = "type-tab-btn" + (idx === currentTypeIndex ? " active" : "");
    btn.innerText = key;
    btn.addEventListener("click", () => {
      currentTypeIndex = idx;
      updateView();
    });
    typeTabsContainer.appendChild(btn);
  });

  // 플레이어 네비게이션 버튼 (◀ / ▶)
  document.getElementById("prevPlayerBtn").addEventListener("click", () => {
    currentPlayerIndex = (currentPlayerIndex - 1 + playerKeys.length) % playerKeys.length;
    updateView();
  });
  document.getElementById("nextPlayerBtn").addEventListener("click", () => {
    currentPlayerIndex = (currentPlayerIndex + 1) % playerKeys.length;
    updateView();
  });

  // 키보드 타입 네비게이션 버튼 (◀ / ▶)
  document.getElementById("prevTypeBtn").addEventListener("click", () => {
    currentTypeIndex = (currentTypeIndex - 1 + keyboardTypeKeys.length) % keyboardTypeKeys.length;
    updateView();
  });
  document.getElementById("nextTypeBtn").addEventListener("click", () => {
    currentTypeIndex = (currentTypeIndex + 1) % keyboardTypeKeys.length;
    updateView();
  });

  // 초기 렌더링
  updateView();
});
