"use client";

import Link from "next/link";
import { Package } from "lucide-react";
import { memo } from "react";

function AppSidebar() {
  return (
    <aside
      className="
        hidden
        lg:block
        bg-[#0f2740] text-white
      "
    >
      <div className="sticky top-0 h-screen overflow-y-auto p-6">
        {/* Branding */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold leading-none">Sistema</h1>
          <p className="text-sm text-gray-400 mt-1">Gerenciamento</p>
        </div>

        {/* Navegação */}
        <nav className="space-y-2">
          <Link
            href="/orders"
            className="
              flex items-center gap-3
              px-4 py-3 rounded-lg
              bg-white/10 text-white font-medium
              hover:bg-white/15 transition-colors
            "
            aria-current="page"
          >
            <Package className="h-5 w-5 shrink-0" />
            <span>Pedidos</span>
          </Link>
        </nav>
      </div>
    </aside>
  );
}

export default memo(AppSidebar);
