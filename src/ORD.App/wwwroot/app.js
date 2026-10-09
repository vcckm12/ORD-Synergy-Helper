let allUnits = [];
let currentDeckIds = [];
let activeTier = 'ALL';

// 페이지 초기 로드
document.addEventListener('DOMContentLoaded', async () => {
  await loadUnits();
  await checkGameStatus();
  setInterval(checkGameStatus, 3000); // 3초마다 실시간 워크래프트3/TMO 상태 체크

  // 초기 로드 시 덱 상태 초기화 렌더링
  if (currentDeckIds.length === 0) {
    evaluateDeck();
  }
});

// 전체 유닛 목록 로드
async function loadUnits() {
  try {
    const res = await fetch('/api/units');
    allUnits = await res.json();
    renderUnitPicker();
  } catch (err) {
    console.error('Failed to load units:', err);
  }
}

// 실시간 워크래프트3 연동 상태 확인
async function checkGameStatus() {
  const dotEl = document.getElementById('gameDot');
  const titleEl = document.getElementById('gameStatusTitle');
  const descEl = document.getElementById('gameStatusDesc');

  try {
    const res = await fetch('/api/game/status');
    const status = await res.json();

    if (status.tmoBridgeConnected) {
      dotEl.className = 'status-dot dot-map';
      titleEl.textContent = '⚡ TMO 데스크탑 연동됨';
      descEl.textContent = '인게임 유닛 실시간 자동 동기화 중';
    } else if (status.isMapActive) {
      dotEl.className = 'status-dot dot-map';
      titleEl.textContent = '🎮 원랜디 v2.323 감지됨';
      descEl.textContent = status.detectedMap || '맵 로드 완료';
    } else if (status.isGameRunning) {
      dotEl.className = 'status-dot dot-running';
      titleEl.textContent = '🎮 워크3 실행 중 (인게임 대기)';
      descEl.textContent = `${status.detectedVersion} (PID: ${status.processId})`;
    } else {
      dotEl.className = 'status-dot dot-idle';
      titleEl.textContent = '스마트 조합 모드 (수동/시뮬)';
      descEl.textContent = '유닛 클릭 시 즉시 1타 2피 & 항법 분석';
    }

    // 인게임 유닛 자동 반영 로직
    if (status.autoDetectedUnits && status.autoDetectedUnits.length > 0) {
      const isDifferent = status.autoDetectedUnits.length !== currentDeckIds.length ||
        !status.autoDetectedUnits.every((id, idx) => id === currentDeckIds[idx]);
      if (isDifferent) {
        currentDeckIds = [...status.autoDetectedUnits];
        renderDeckChips();
        renderUnitPicker();
        await evaluateDeck();
      }
    }
  } catch (err) {
    dotEl.className = 'status-dot dot-idle';
    titleEl.textContent = '연동 확인 불가';
    descEl.textContent = '로컬 서버 확인 필요';
  }
}

// 덱 평가 및 추천 요청
async function evaluateDeck() {
  try {
    const res = await fetch('/api/evaluate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ unitIds: currentDeckIds, targetRound: 65 })
    });

    const data = await res.json();
    updateDashboardUI(data);
  } catch (err) {
    console.error('Failed to evaluate deck:', err);
  }
}

// UI 업데이트
function updateDashboardUI(data) {
  const { analysis, recommendations, currentUnits } = data;

  // 1. 메인 캐리 및 덱 타입
  const carryNameEl = document.getElementById('mainCarryName');
  const deckTypeBadge = document.getElementById('deckTypeBadge');
  const scoreValEl = document.getElementById('scoreValue');

  if (analysis.mainCarry) {
    carryNameEl.textContent = analysis.mainCarry.name;
    deckTypeBadge.textContent = analysis.primaryType === 1 ? '물리 덱 (물딜)' : (analysis.primaryType === 2 ? '마법 덱 (마딜)' : '하이브리드 덱');
    deckTypeBadge.className = analysis.primaryType === 1 ? 'badge badge-physical' : 'badge badge-magic';
  } else {
    carryNameEl.textContent = '- 필드에 유닛이 없습니다 -';
    deckTypeBadge.textContent = '덱 미정';
    deckTypeBadge.className = 'badge';
  }

  // 점수
  scoreValEl.textContent = Math.round(analysis.nightmareClearProbabilityScore);
  if (analysis.nightmareClearProbabilityScore >= 90) {
    scoreValEl.style.color = '#10b981'; // 초록
  } else if (analysis.nightmareClearProbabilityScore >= 60) {
    scoreValEl.style.color = '#f59e0b'; // 주황
  } else {
    scoreValEl.style.color = '#ef4444'; // 빨강
  }

  // 2. 4대 게이지
  // 방깎 (목표 211)
  const armorValEl = document.getElementById('armorVal');
  const armorBar = document.getElementById('armorBar');
  const armorSub = document.getElementById('armorSub');
  const totalArmor = analysis.totalArmorReduction;
  armorValEl.textContent = totalArmor;
  const armorPct = Math.min(100, (totalArmor / 211) * 100);
  armorBar.style.width = `${armorPct}%`;
  armorSub.textContent = `기본 오라 ${analysis.totalArmorReduction} + 암브 ${analysis.totalArmorBreak} (v2.323 악몽 목표 211)`;

  // 이감 (목표 102%)
  const slowValEl = document.getElementById('slowVal');
  const slowBar = document.getElementById('slowBar');
  const slowSub = document.getElementById('slowSub');
  slowValEl.textContent = `${analysis.totalSlow}%`;
  const slowPct = Math.min(100, (analysis.totalSlow / 102) * 100);
  slowBar.style.width = `${slowPct}%`;
  if (analysis.totalSlow >= 102) {
    slowSub.textContent = `★ 풀이감 102% 완결! (몹 완벽 고정)`;
    slowSub.style.color = '#38bdf8';
  } else {
    slowSub.textContent = `풀이감 102%까지 ${102 - analysis.totalSlow}% 필요`;
    slowSub.style.color = '#94a3b8';
  }

  // 스턴 (목표 2.0)
  const stunValEl = document.getElementById('stunVal');
  const stunBar = document.getElementById('stunBar');
  const stunSub = document.getElementById('stunSub');
  stunValEl.textContent = analysis.totalStun.toFixed(1);
  const stunPct = Math.min(100, (analysis.totalStun / 2.0) * 100);
  stunBar.style.width = `${stunPct}%`;
  if (analysis.totalStun < 2.0) {
    stunSub.textContent = `위험: 최소 2.0 스턴 필요 (라인 흘림 방지)`;
    stunSub.style.color = '#f87171';
  } else if (analysis.totalStun <= 2.5) {
    stunSub.textContent = `적정: 2.0~2.5 안정권 스턴`;
    stunSub.style.color = '#10b981';
  } else {
    stunSub.textContent = `주의: 2.5 초과 스턴 과투자 (방깎/이감 딜로스 주의)`;
    stunSub.style.color = '#f59e0b';
  }

  // 3. 결손(Deficit) 리스트
  const deficitsList = document.getElementById('deficitsList');
  deficitsList.innerHTML = '';
  if (analysis.deficits && analysis.deficits.length > 0) {
    analysis.deficits.forEach(d => {
      const li = document.createElement('li');
      li.textContent = d;
      if (d.includes('치명적') || d.includes('위험')) {
        li.className = 'critical';
      } else if (d.includes('달성')) {
        li.className = 'success';
      }
      deficitsList.appendChild(li);
    });
  } else {
    deficitsList.innerHTML = '<li class="success">모든 악몽 클리어 시너지가 충족되었습니다!</li>';
  }

  // 4. AI 스마트 추천 리스트
  const recListEl = document.getElementById('recList');
  recListEl.innerHTML = '';
  if (recommendations && recommendations.length > 0) {
    recommendations.slice(0, 5).forEach((r, idx) => {
      const item = document.createElement('div');
      item.className = `rec-item rank-${idx + 1}`;

      const twoInOneBadge = r.isTwoInOne ? '<span class="badge-two-in-one">★ 1타 2피</span>' : '';
      
      let missingCommonsHtml = '';
      if (r.missingCommons && Object.keys(r.missingCommons).length > 0) {
        const chips = Object.entries(r.missingCommons)
          .slice(0, 5)
          .map(([name, count]) => `<span class="chip-missing">${name} ${count}</span>`)
          .join(' ');
        missingCommonsHtml = `<div class="missing-commons-wrap"><span class="missing-title">부족 흔함:</span> ${chips}</div>`;
      }

      let directUnitsHtml = '';
      if (r.missingDirectUnits && r.missingDirectUnits.length > 0) {
        directUnitsHtml = `<div class="missing-direct-units">필요 하위패: ${r.missingDirectUnits.slice(0, 3).join(', ')}</div>`;
      }

      item.innerHTML = `
        <div class="rec-left" style="flex: 1;">
          <div style="display: flex; align-items: center; gap: 8px;">
            <span class="rec-rank-badge">#${idx + 1}</span>
            <div>
              <span class="rec-unit-name">${r.targetUnit.name}</span>
              <span class="rec-tier-badge">${tierName(r.targetUnit.tier)}</span>
              ${twoInOneBadge}
            </div>
          </div>
          <div class="rec-reason" style="margin-top: 4px;">${r.coreReason}</div>
          
          <!-- 하위 조합 완성도 게이지 & 부족한 흔함 패 -->
          <div class="readiness-bar-wrap">
            <div class="readiness-header-row">
              <span>조합 완성도: <strong>${Math.round(r.recipeReadiness)}%</strong></span>
              <span style="color: #94a3b8">남은 흔함: <strong>${r.totalMissingCommons}개</strong></span>
            </div>
            <div class="progress-track" style="height: 6px;">
              <div class="progress-fill fill-readiness" style="width: ${r.recipeReadiness}%"></div>
            </div>
            ${missingCommonsHtml}
            ${directUnitsHtml}
          </div>
        </div>
        <div class="rec-right" style="margin-left: 12px; display: flex; flex-direction: column; align-items: flex-end; justify-content: center;">
          <div class="rec-score">${Math.round(r.recommendationScore)}점</div>
          <button class="btn-add-rec" onclick="addUnit('${r.targetUnit.id}')">+ 필드 추가</button>
        </div>
      `;
      recListEl.appendChild(item);
    });
  } else {
    recListEl.innerHTML = '<div class="rec-empty">추천할 유닛이 없거나 이미 풀스펙입니다.</div>';
  }

  // 4-1. 21라운드 항법 추천 렌더링
  const navRecListEl = document.getElementById('navRecList');
  if (navRecListEl && data.navigationRecommendations) {
    navRecListEl.innerHTML = '';
    data.navigationRecommendations.slice(0, 3).forEach(nr => {
      const item = document.createElement('div');
      item.className = 'nav-rec-item';
      item.innerHTML = `
        <div class="nav-title-row">
          <span class="nav-name">👑 ${nr.style.name}</span>
          <span class="nav-score">적합도 ${Math.round(nr.matchScore)}%</span>
        </div>
        <div class="nav-rationale">${nr.rationale}</div>
        <div class="nav-tip">💡 실전 팁: ${nr.practicalTip}</div>
      `;
      navRecListEl.appendChild(item);
    });
  }

  // 5. 현재 보유 덱 카드 태그 렌더링
  const currentDeckContainer = document.getElementById('currentDeckContainer');
  const deckCountEl = document.getElementById('deckCount');
  currentDeckContainer.innerHTML = '';
  deckCountEl.textContent = currentUnits.length;

  if (currentUnits.length > 0) {
    currentUnits.forEach(u => {
      const tag = document.createElement('div');
      tag.className = 'deck-tag';
      tag.innerHTML = `<span>${u.name}</span> <span class="remove-icon">×</span>`;
      tag.onclick = () => removeUnit(u.id);
      currentDeckContainer.appendChild(tag);
    });
  } else {
    currentDeckContainer.innerHTML = '<div class="deck-empty">도감에서 유닛을 클릭해 추가하세요.</div>';
  }

  // 6. 오로성 상태 갱신
  updateGoroseiCards(totalArmor, analysis.totalSlow, analysis.primaryType);
}

// 오로성 직관 설명 갱신
function updateGoroseiCards(armor, slow, primaryType) {
  const colW = document.getElementById('colWarcury');
  const colN = document.getElementById('colNusjuro');
  const colS = document.getElementById('colSaturn');
  const descW = document.getElementById('descWarcury');
  const descN = document.getElementById('descNusjuro');
  const descS = document.getElementById('descSaturn');

  const afterW = armor + 15;
  const afterN = slow + 15;

  // 워큐리 (-15깎, -15마방깎)
  if (armor >= 211) {
    descW.innerHTML = `<span style="color: #ef4444">이미 211 풀방깎 달성! (+15깎 효율 감쇠로 비추천)</span>`;
  } else {
    descW.innerHTML = `<span style="color: #38bdf8">현재 ${armor}깎 → <strong>${afterW}깎</strong> (풀방깎 보완 기여)</span>`;
  }

  // 나스쥬로 (-15이감, -15공속)
  if (slow < 102) {
    descN.innerHTML = `<span style="color: #10b981">현재 ${slow}% → <strong>${afterN}%</strong> (★풀이감 102% 즉시 완성!)</span>`;
  } else {
    descN.innerHTML = `<span style="color: #38bdf8">현재 ${slow}% → <strong>${afterN}%</strong> (117% 몹 스폰존 완전 고정)</span>`;
  }

  // 새턴 (+10% 폭뎀증, 체젠 35만 억제, 아군 공증 30%)
  if (descS) {
    if (primaryType === 2) {
      descS.innerHTML = `<span style="color: #a855f7">★마딜 특화: 폭뎀 10% 증폭 + 적 체젠 35만 억제 극대화</span>`;
    } else {
      descS.innerHTML = `<span style="color: #e2e8f0">아군 공증 30% + 적 체젠 35만 억제 (고체력 보스 녹이기)</span>`;
    }
  }
}

// 유닛 도감 피커 렌더링
function renderUnitPicker() {
  const grid = document.getElementById('unitPickerGrid');
  const search = document.getElementById('unitSearch').value.toLowerCase().trim();

  grid.innerHTML = '';

  const filtered = allUnits.filter(u => {
    const matchSearch = u.name.toLowerCase().includes(search) || u.id.toLowerCase().includes(search);
    
    let matchTier = false;
    if (activeTier === 'ALL') matchTier = true;
    else if (activeTier === 'Transcendence') matchTier = u.tier === 9;
    else if (activeTier === 'Immortal') matchTier = u.tier === 10;
    else if (activeTier === 'Eternal') matchTier = u.tier === 11;
    else if (activeTier === 'Limited') matchTier = u.tier === 8;
    else if (activeTier === 'Legendary') matchTier = u.tier === 7;
    else if (activeTier === 'Hidden') matchTier = u.tier === 5 || u.tier === 6; // 히든 + 변화
    else if (activeTier === 'Rare') matchTier = u.tier === 4;
    else if (activeTier === 'Special') matchTier = u.tier === 3;
    else if (activeTier === 'Common') matchTier = u.tier === 1 || u.tier === 2; // 흔함 + 안흔함
    else if (activeTier === 'Gorosei') matchTier = u.tier === 13;

    return matchSearch && matchTier;
  });

  filtered.forEach(u => {
    const card = document.createElement('div');
    card.className = 'unit-card';
    card.onclick = () => addUnit(u.id);

    const statsText = [];
    if (u.synergy.armorReduction > 0) statsText.push(`${u.synergy.armorReduction}깎`);
    if (u.synergy.armorBreakOverlap > 0) statsText.push(`암브${u.synergy.armorBreakOverlap}`);
    if (u.synergy.movementSlow > 0) statsText.push(`이감${u.synergy.movementSlow}%`);
    if (u.synergy.stunValue > 0) statsText.push(`${u.synergy.stunValue}스턴`);
    if (u.synergy.magicArmorReduction > 0) statsText.push(`마깎${u.synergy.magicArmorReduction}%`);

    const commonsCountText = u.totalRequiredCommons > 0 ? `흔함 ${u.totalRequiredCommons}개` : '';

    card.innerHTML = `
      <div class="card-title-row">
        <span class="card-name">${u.name}</span>
        <span class="card-tier">${tierName(u.tier)}</span>
      </div>
      <div class="card-stats-row">
        ${statsText.join(' · ') || (commonsCountText || '서포터')}
      </div>
    `;
    grid.appendChild(card);
  });
}

function tierName(tier) {
  const map = {
    1: '흔함', 2: '안흔함', 3: '특별함', 4: '희귀함', 5: '히든',
    6: '변화', 7: '전설', 8: '제한', 9: '초월', 10: '불멸',
    11: '영원', 12: '랜덤', 13: '오로성'
  };
  return map[tier] || tier;
}

function filterUnits() {
  renderUnitPicker();
}

function setTierFilter(tier) {
  activeTier = tier;
  document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
  event.target.classList.add('active');
  renderUnitPicker();
}

// 덱 조작
function addUnit(id) {
  if (!currentDeckIds.includes(id)) {
    currentDeckIds.push(id);
    evaluateDeck();
  }
}

function removeUnit(id) {
  currentDeckIds = currentDeckIds.filter(x => x !== id);
  evaluateDeck();
}

function toggleUnit(id) {
  if (currentDeckIds.includes(id)) {
    removeUnit(id);
  } else {
    addUnit(id);
  }
}

function resetDeck() {
  currentDeckIds = [];
  evaluateDeck();
}

// 빠른 프리셋
function loadPreset(presetName) {
  switch (presetName) {
    case 'luffy':
      currentDeckIds = ['TR8']; // 루피 초월
      break;
    case 'jinbe_king':
      currentDeckIds = ['TR22', 'Z10']; // 징베 초월 + 킹 제한
      break;
    case 'question_case':
      // 211깎 · 102이감 유저 질문 케이스
      currentDeckIds = ['TR8', 'L21', 'H10', 'D5', 'L37', 'L4', 'gorosei_nusjuro'];
      break;
    case 'shirahoshi':
      currentDeckIds = ['TR15', 'SR4', 'TR16']; // 시라호시 초월 + 쿠라핌 + 아오키지 초월
      break;
  }
  evaluateDeck();
}
