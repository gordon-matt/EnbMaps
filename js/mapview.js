// Pan/zoom for map viewports. The view transform is exposed to CSS as custom properties
// (--s: pixels per map unit, --tx/--ty: screen position of the map origin) so panning and
// zooming never require a Blazor re-render.

const DRAG_THRESHOLD = 4;
const TOOLTIP_OFFSET = 10;
const TOOLTIP_MARGIN = 5;

class MapView {
    constructor(element, options) {
        this.el = element;
        this.yUp = options.yUp;
        this.rect = options.rect;
        this.pointers = new Map();
        this.dragged = false;
        this.s = 1;
        this.tx = 0;
        this.ty = 0;

        this.onPointerDown = this.onPointerDown.bind(this);
        this.onPointerMove = this.onPointerMove.bind(this);
        this.onPointerUp = this.onPointerUp.bind(this);
        this.onWheel = this.onWheel.bind(this);
        this.onClickCapture = this.onClickCapture.bind(this);
        this.onDoubleClick = this.onDoubleClick.bind(this);
        this.onMouseMove = this.onMouseMove.bind(this);

        element.addEventListener('pointerdown', this.onPointerDown);
        element.addEventListener('pointermove', this.onPointerMove);
        element.addEventListener('pointerup', this.onPointerUp);
        element.addEventListener('pointercancel', this.onPointerUp);
        element.addEventListener('wheel', this.onWheel, { passive: false });
        element.addEventListener('click', this.onClickCapture, true);
        element.addEventListener('dblclick', this.onDoubleClick);
        element.addEventListener('mousemove', this.onMouseMove);

        this.resizeObserver = new ResizeObserver(() => this.onResize());
        this.resizeObserver.observe(element);
        this.width = element.clientWidth;
        this.height = element.clientHeight;
        this.fit();
    }

    apply() {
        this.el.style.setProperty('--s', this.s);
        this.el.style.setProperty('--tx', this.tx);
        this.el.style.setProperty('--ty', this.ty);
    }

    computeFitScale() {
        const r = this.rect;
        const w = Math.max(r.maxX - r.minX, 1);
        const h = Math.max(r.maxY - r.minY, 1);
        return Math.min(this.width / w, this.height / h);
    }

    fit() {
        const r = this.rect;
        this.fitScale = this.computeFitScale();
        this.s = this.fitScale;
        this.centerOnPoint((r.minX + r.maxX) / 2, (r.minY + r.maxY) / 2);
    }

    centerOnPoint(x, y) {
        this.tx = this.width / 2 - x * this.s;
        this.ty = this.yUp ? this.height / 2 + y * this.s : this.height / 2 - y * this.s;
        this.apply();
    }

    centerOn(x, y, zoom) {
        if (zoom) {
            this.s = this.clampScale(this.fitScale * zoom);
        }
        this.centerOnPoint(x, y);
    }

    clampScale(s) {
        return Math.min(Math.max(s, this.fitScale * 0.25), this.fitScale * 40);
    }

    zoomAt(px, py, factor) {
        const s = this.clampScale(this.s * factor);
        const k = s / this.s;
        this.tx = px - (px - this.tx) * k;
        this.ty = py - (py - this.ty) * k;
        this.s = s;
        this.apply();
    }

    zoomBy(factor) {
        this.zoomAt(this.width / 2, this.height / 2, factor);
    }

    onResize() {
        const w = this.el.clientWidth;
        const h = this.el.clientHeight;
        if (w === this.width && h === this.height) {
            return;
        }
        // Keep the current centre in place when the viewport changes size.
        this.tx += (w - this.width) / 2;
        this.ty += (h - this.height) / 2;
        this.width = w;
        this.height = h;
        this.fitScale = this.computeFitScale();
        this.apply();
    }

    localPoint(e) {
        const r = this.el.getBoundingClientRect();
        return { x: e.clientX - r.left, y: e.clientY - r.top };
    }

    onPointerDown(e) {
        if (e.pointerType === 'mouse' && e.button !== 0) {
            return;
        }
        if (e.target.closest('.map-ui')) {
            return;
        }
        this.pointers.set(e.pointerId, this.localPoint(e));
        if (this.pointers.size === 1) {
            this.dragged = false;
            this.dragStart = this.localPoint(e);
        }
    }

    onPointerMove(e) {
        if (!this.pointers.has(e.pointerId)) {
            return;
        }
        const previous = this.pointers.get(e.pointerId);
        const current = this.localPoint(e);

        if (this.pointers.size === 1) {
            if (!this.dragged) {
                const dx = current.x - this.dragStart.x;
                const dy = current.y - this.dragStart.y;
                if (Math.hypot(dx, dy) < DRAG_THRESHOLD) {
                    return;
                }
                this.dragged = true;
                this.el.setPointerCapture(e.pointerId);
                this.el.classList.add('dragging');
            }
            this.tx += current.x - previous.x;
            this.ty += current.y - previous.y;
            this.pointers.set(e.pointerId, current);
            this.apply();
            return;
        }

        // Pinch zoom: scale by the change in distance between two pointers, around their midpoint.
        const [a, b] = [...this.pointers.entries()].map(([id, p]) => id === e.pointerId ? current : p);
        const [pa, pb] = [...this.pointers.values()];
        const before = Math.hypot(pa.x - pb.x, pa.y - pb.y);
        const after = Math.hypot(a.x - b.x, a.y - b.y);
        this.pointers.set(e.pointerId, current);
        this.dragged = true;
        if (before > 0) {
            const midX = (a.x + b.x) / 2;
            const midY = (a.y + b.y) / 2;
            this.tx += (current.x - previous.x) / 2;
            this.ty += (current.y - previous.y) / 2;
            this.zoomAt(midX, midY, after / before);
        }
    }

    onPointerUp(e) {
        this.pointers.delete(e.pointerId);
        if (this.el.hasPointerCapture(e.pointerId)) {
            this.el.releasePointerCapture(e.pointerId);
        }
        if (this.pointers.size === 0) {
            this.el.classList.remove('dragging');
        }
    }

    onWheel(e) {
        if (e.target.closest('.map-ui')) {
            return;
        }
        e.preventDefault();
        const p = this.localPoint(e);
        this.zoomAt(p.x, p.y, Math.exp(-e.deltaY * 0.0015));
    }

    onDoubleClick(e) {
        if (e.target.closest('.map-ui, a, .map-nav')) {
            return;
        }
        const p = this.localPoint(e);
        this.zoomAt(p.x, p.y, e.shiftKey ? 0.5 : 2);
    }

    // A drag must not also count as a click on whatever is under the pointer (links, navs).
    onClickCapture(e) {
        if (this.dragged) {
            e.preventDefault();
            e.stopPropagation();
            this.dragged = false;
        }
    }

    // Positions the tooltip box next to the mouse, flipping sides near the edges like the original site.
    onMouseMove(e) {
        const box = this.el.querySelector('.mouseoverbox');
        if (!box) {
            return;
        }
        const p = this.localPoint(e);
        const w = box.offsetWidth;
        const h = box.offsetHeight;
        let x = p.x + TOOLTIP_OFFSET;
        let y = p.y + TOOLTIP_OFFSET;
        if (x + w + TOOLTIP_MARGIN > this.width) {
            x = Math.max(TOOLTIP_MARGIN, p.x - TOOLTIP_OFFSET - w);
        }
        if (y + h + TOOLTIP_MARGIN > this.height) {
            y = Math.max(TOOLTIP_MARGIN, p.y - TOOLTIP_OFFSET - h);
        }
        box.style.left = `${x}px`;
        box.style.top = `${y}px`;
    }

    dispose() {
        this.resizeObserver.disconnect();
        this.el.removeEventListener('pointerdown', this.onPointerDown);
        this.el.removeEventListener('pointermove', this.onPointerMove);
        this.el.removeEventListener('pointerup', this.onPointerUp);
        this.el.removeEventListener('pointercancel', this.onPointerUp);
        this.el.removeEventListener('wheel', this.onWheel);
        this.el.removeEventListener('click', this.onClickCapture, true);
        this.el.removeEventListener('dblclick', this.onDoubleClick);
        this.el.removeEventListener('mousemove', this.onMouseMove);
    }
}

export function create(element, options) {
    return new MapView(element, options);
}
